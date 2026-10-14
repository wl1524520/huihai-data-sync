using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Newtonsoft.Json.Linq;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 成绩上传(本地 → 平台),移植自 unite-data-sync 的 upload_scores,按本软件方案调整:
    ///   - 数据源为唯一成绩表 StudentRawScore 中 Uploaded=0 的行,原样全传(不去重)
    ///   - 仅接口 code 映射表内的科目(1,2,4,5,6,7,10,11,12,14)上传,code = TestID + 10000;
    ///     表外科目留在本地底账;SpecialType 非空的特殊成绩不上传(与原工具一致)
    ///   - 每批 1000 条;网络/限流/5xx 失败不原地重试,直接传下一批,下轮自然重传;
    ///     200+success=false 与 422 按文档 §6.2/§7 停止本轮,重启后原样补传
    ///   - 仅当 HTTP 200 + JSON success===true 才在事务内把该批标记 Uploaded=1
    ///   - 401/400 停止本轮
    /// </summary>
    public class ScoreUpload
    {
        public const int BatchSize = 1000;
        public const int RoundIntervalSeconds = 120;

        /// <summary>接口有 code 映射的科目白名单(本地 TestID)</summary>
        private static readonly int[] UploadableItems = { 1, 2, 4, 5, 6, 7, 10, 11, 12, 14 };

        private readonly SyncApi _api;
        private readonly string _projectID;

        public ScoreUpload(SyncApi api, string projectID)
        {
            _api = api;
            _projectID = projectID;
        }

        /// <summary>上传一轮:取当前项目全部未标记成绩,分批上传。返回 (成功条数, 失败条数)。</summary>
        public Tuple<int, int> RunOnce(Action<string> report, Func<bool> stopRequested)
        {
            var info = _api.GetSyncInfo(true);
            report(info.ToString());
            report("本地项目:" + _projectID + "(上传目标项目由平台当前配置决定,请核对上面的远端项目)");

            var pending = GetPendingScores();
            report("待上传成绩 " + pending.Count + " 条");
            int sent = 0, failed = 0;
            for (int offset = 0; offset < pending.Count && !stopRequested(); offset += BatchSize)
            {
                var batch = pending.GetRange(offset, Math.Min(BatchSize, pending.Count - offset));
                try
                {
                    _api.PostScores(BuildItems(batch));
                    MarkUploaded(batch);
                    sent += batch.Count;
                }
                catch (SyncAuthException)
                {
                    throw; // 令牌失效:停止本轮
                }
                catch (SyncFatalException)
                {
                    throw; // 学校/项目不存在:停止本轮
                }
                catch (SyncBatchRejectedException ex)
                {
                    // 部分明细保存失败(success=false)或字段验证失败(422):
                    // 本批不标记,停止本轮;重启后从未标记数据原样补传(文档 §6.2/§7)
                    failed += batch.Count;
                    report("服务端拒收,停止本轮:" + ex.Message);
                    break;
                }
                catch (SyncException ex)
                {
                    // 网络/限流/5xx 等批次失败:不原地重试,直接下一批;本批保持未标记
                    failed += batch.Count;
                    report("批次上传失败(跳过,下轮重传):" + ex.Message);
                }
                report(string.Format("[{0:0.00}%] {1} of {2}", (sent + failed) * 100.0 / pending.Count, sent + failed, pending.Count));
            }
            report(string.Format("本轮上传完成:成功 {0} 条,失败 {1} 条", sent, failed));
            return Tuple.Create(sent, failed);
        }

        private List<RawScoreRow> GetPendingScores()
        {
            var list = new List<RawScoreRow>();
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT HEX(ID) AS Mono, StudentID, TestID, Score, CreateTime FROM StudentRawScore " +
                        "WHERE ProjectID = @p AND Uploaded = 0 AND SpecialType IS NULL AND Score IS NOT NULL " +
                        "AND TestID IN (" + string.Join(",", UploadableItems) + ") " +
                        "ORDER BY CreateTime";
                    cmd.Parameters.AddWithValue("@p", _projectID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new RawScoreRow
                            {
                                Mono = Convert.ToString(reader["Mono"]),
                                StudentID = Convert.ToString(reader["StudentID"]),
                                TestID = Convert.ToInt32(reader["TestID"]),
                                Score = reader["Score"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Score"]),
                                CreateTime = Convert.ToDateTime(reader["CreateTime"]),
                            });
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>组装接口载荷:每行仅 number/code/score/modifier/completed_at 五字段。</summary>
        private static JArray BuildItems(List<RawScoreRow> batch)
        {
            var items = new JArray();
            foreach (var row in batch)
            {
                if (row.Score == null) continue; // 无成绩值的记录不上传(接口必填 score)
                var item = new JObject();
                item["number"] = row.StudentID;
                item["code"] = row.TestID + 10000;
                // score 用普通十进制字符串保留精度(文档 §4 建议),本地表无 modifier 列,恒发 null(按空串处理)
                item["score"] = row.Score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                item["modifier"] = null;
                item["completed_at"] = row.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
                items.Add(item);
            }
            return items;
        }

        /// <summary>ACK 成功后,在单个事务内把本批标记为已上传。</summary>
        private void MarkUploaded(List<RawScoreRow> batch)
        {
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "UPDATE StudentRawScore SET Uploaded = 1 WHERE HEX(ID) = @mono";
                    var p = cmd.Parameters.Add("@mono", System.Data.DbType.String);
                    foreach (var row in batch)
                    {
                        p.Value = row.Mono;
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        /// <summary>重置上传标记:清当前项目全部 Uploaded,用于补传。</summary>
        public static int ResetUploaded(string projectID)
        {
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE StudentRawScore SET Uploaded = 0 WHERE ProjectID = @p";
                    cmd.Parameters.AddWithValue("@p", projectID);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>统计当前项目待上传/已上传条数(界面展示用)。</summary>
        public static Tuple<int, int> CountByUploaded(string projectID)
        {
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Uploaded, COUNT(*) FROM StudentRawScore WHERE ProjectID = @p GROUP BY Uploaded";
                    cmd.Parameters.AddWithValue("@p", projectID);
                    int pending = 0, done = 0;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (Convert.ToInt32(reader[0]) == 0) pending = Convert.ToInt32(reader[1]);
                            else done = Convert.ToInt32(reader[1]);
                        }
                    }
                    return Tuple.Create(pending, done);
                }
            }
        }

        private class RawScoreRow
        {
            public string Mono;
            public string StudentID;
            public int TestID;
            public decimal? Score;
            public DateTime CreateTime;
        }
    }
}
