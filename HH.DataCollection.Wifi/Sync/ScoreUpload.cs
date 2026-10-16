using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Newtonsoft.Json.Linq;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 成绩上传(本地 → 平台),移植自 unite-data-sync 的 upload_scores,按本软件方案调整:
    ///   - 数据源为唯一成绩表 StudentRawScore 中 Uploaded=0 的行,原样全传(不去重)
    ///   - 仅接口 code 映射表内的科目上传,code = TestID + 10000(映射依据已核对
    ///     lib/DbUpdate_SQLITE.sql 种子字典与 2026-10-16 版接口文档 §5.1 code 表一致);
    ///     表外科目留在本地底账;SpecialType 非空的特殊成绩不上传(与原工具一致)
    ///   - 每批 1000 条;网络错误/5xx 由 SyncApi 有限退避后仍失败则跳过本批传下一批,下轮自然重传;
    ///     429 退避后仍未缓解停止本轮;200+success=false 与 422 按文档 §4/§7 停止本轮,重启后原样补传
    ///   - 仅当 HTTP 200 + JSON success===true 才在事务内把该批标记 Uploaded=1
    ///   - 401/400 停止本轮;上传前按文档 §4/§5 做本地预校验,非法行隔离不上传、不阻断其他成绩
    /// </summary>
    public class ScoreUpload
    {
        public const int BatchSize = 1000;
        public const int RoundIntervalSeconds = 120;

        /// <summary>
        /// 接口有 code 映射的科目白名单(本地 TestID)。
        /// 对应平台 code:10001身高/10002体重/10004肺活量/10005 50米/10006立定跳远/10007坐位体前屈/
        /// 10009 50米×8往返/10010 800米/10011 1000米/10012仰卧起坐/10014引体向上/10018跳绳。
        /// 种子字典另有 24(BMI),文档明确禁止上传,不在白名单。
        /// </summary>
        private static readonly int[] UploadableItems = { 1, 2, 4, 5, 6, 7, 9, 10, 11, 12, 14, 18 };

        /// <summary>分.秒(m.ss)编码的科目(文档 §5.3:10009/10010/10011,服务器不猜编码)</summary>
        private static readonly int[] MinSecItems = { 9, 10, 11 };

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
            // 平台项目变更检测(文档 §2/§8:年度切换应暂停同步核对):
            // 保存设置时记录的远端项目与当前 info 不一致 → 停止本轮,避免成绩发进新项目而本地底账在旧项目
            var conf = SyncConfig.Load();
            if (!string.IsNullOrWhiteSpace(conf.RemoteProjectId) && conf.RemoteProjectId != info.ProjectId)
                throw new SyncFatalException(string.Format(
                    "平台当前项目已切换(原:{0}/ID:{1} → 现:{2}/ID:{3}),停止本轮;请核对后重新保存同步设置",
                    conf.RemoteProjectName, conf.RemoteProjectId, info.ProjectName, info.ProjectId));
            if (string.IsNullOrWhiteSpace(conf.RemoteProjectId))
                report("提示:尚未记录远端项目基线(旧版设置),重新保存一次同步设置即可启用项目变更检测");
            report("本地项目:" + _projectID);

            var pending = GetPendingScores();
            report("待上传成绩 " + pending.Count + " 条");
            IsolateInvalidRows(pending, report);
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
                    // 本批不标记,停止本轮;重启后从未标记数据原样补传(文档 §4/§7)
                    failed += batch.Count;
                    report("服务端拒收,停止本轮:" + ex.Message);
                    break;
                }
                catch (SyncRateLimitedException ex)
                {
                    // 429 退避后仍未缓解:停止本轮,避免继续打后续批次加剧限流(文档 §7)
                    failed += batch.Count;
                    report(ex.Message);
                    break;
                }
                catch (SyncException ex)
                {
                    // 网络/5xx 经退避后仍失败:跳过本批传下一批;本批保持未标记,下轮重传
                    failed += batch.Count;
                    report("批次上传失败(跳过,下轮重传):" + ex.Message);
                }
                report(string.Format("[{0:0.00}%] {1} of {2}", (sent + failed) * 100.0 / pending.Count, sent + failed, pending.Count));
            }
            report(string.Format("本轮上传完成:成功 {0} 条,失败 {1} 条", sent, failed));
            return Tuple.Create(sent, failed);
        }

        /// <summary>
        /// 上传前本地预校验(文档 §4/§5.3):number 非空 ≤50、|score| ≤99999999.99、
        /// 分.秒项目秒位 00~59。非法行从待传列表移除并逐条报告——不预检的话一行脏数据
        /// 会让整批 422,且按 CreateTime 排序后永远卡在队首堵死全部上传("毒行")。
        /// 隔离行保持 Uploaded=0,每轮重复报告,直到原始数据被修正。
        /// </summary>
        private static void IsolateInvalidRows(List<RawScoreRow> pending, Action<string> report)
        {
            int isolated = 0;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                string reason = ValidateRow(pending[i]);
                if (reason == null) continue;
                isolated++;
                if (isolated <= 20)
                {
                    var row = pending[i];
                    report(string.Format("已隔离非法成绩:学号 {0} 科目 {1} 成绩 {2} — {3}",
                        row.StudentID, row.TestID + 10000, row.Score, reason));
                }
                pending.RemoveAt(i);
            }
            if (isolated > 0)
                report(string.Format("共隔离 {0} 条不符合接口格式要求的成绩(不上传、不阻断其他成绩;请修正原始数据)", isolated));
        }

        /// <summary>返回该行违反接口格式的原因,合法返回 null。Score 已由查询保证非空。</summary>
        private static string ValidateRow(RawScoreRow row)
        {
            if (string.IsNullOrWhiteSpace(row.StudentID) || row.StudentID.Length > 50)
                return "学号为空或超过 50 字符";
            decimal score = row.Score.Value;
            if (Math.Abs(score) > 99999999.99m)
                return "成绩绝对值超过 99999999.99";
            if (Array.IndexOf(MinSecItems, row.TestID) >= 0)
            {
                // m.ss:小数部分即秒数,必须为 00~59 的整秒(本地 decimal(18,4),如 3.45 = 3分45秒)
                decimal cents = (score - Math.Truncate(score)) * 100m;
                if (cents != Math.Truncate(cents) || cents < 0 || cents > 59)
                    return "分.秒成绩秒位必须为 00~59(m.ss 编码)";
            }
            return null;
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
                        "WHERE ProjectID = @p AND Uploaded = 0 AND " + UploadableWhere + " " +
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

        /// <summary>
        /// 组装接口载荷:每行仅 number/code/score/completed_at 四字段。
        /// 2026-10-16 版契约(§4):modifier 必须不存在,null/空串也会 422 整批拒收。
        /// </summary>
        private static JArray BuildItems(List<RawScoreRow> batch)
        {
            var items = new JArray();
            foreach (var row in batch)
            {
                if (row.Score == null) continue; // 无成绩值的记录不上传(接口必填 score)
                var item = new JObject();
                item["number"] = row.StudentID;
                item["code"] = row.TestID + 10000;
                // score 用普通十进制字符串保留精度(文档 §4 建议)
                item["score"] = row.Score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

        /// <summary>
        /// 可上传成绩的过滤条件,与 UploadableItems 白名单同一份来源:
        /// 白名单科目 + 非特殊成绩 + 有成绩值。统计/重置必须与上传同口径,
        /// 否则永不上传的行(白名单外科目、特殊成绩)会永远计入"待上传"误导现场。
        /// </summary>
        private static readonly string UploadableWhere =
            "SpecialType IS NULL AND Score IS NOT NULL AND TestID IN (" + string.Join(",", UploadableItems) + ")";

        /// <summary>重置上传标记:清当前项目全部可上传成绩的 Uploaded,用于补传。</summary>
        public static int ResetUploaded(string projectID)
        {
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE StudentRawScore SET Uploaded = 0 WHERE ProjectID = @p AND " + UploadableWhere;
                    cmd.Parameters.AddWithValue("@p", projectID);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>统计当前项目待上传/已上传条数(与上传同口径,界面展示用)。</summary>
        public static Tuple<int, int> CountByUploaded(string projectID)
        {
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                int pending = CountUploadable(conn, projectID, 0);
                int done = CountUploadable(conn, projectID, 1);
                return Tuple.Create(pending, done);
            }
        }

        private static int CountUploadable(SQLiteConnection conn, string projectID, int uploaded)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM StudentRawScore WHERE ProjectID = @p " +
                                  "AND Uploaded = " + uploaded + " AND " + UploadableWhere;
                cmd.Parameters.AddWithValue("@p", projectID);
                return Convert.ToInt32(cmd.ExecuteScalar());
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
