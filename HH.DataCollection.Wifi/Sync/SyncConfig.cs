using System;
using System.Collections.Generic;
using System.Data.SQLite;
using HH.DataCollection.Wifi.Db;
using HH.TiYu.DAL;
using HH.TiYu.DAL.LinqProvider;
using HH.TiYu.Model;
using HH.GeneralLibrary.Core.DAL;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 同步配置,存于本地库 SyncConfig 表(沿用 unite-data-sync 的配置模型)。
    /// </summary>
    public class SyncConfig
    {
        public string ApiUrl = "";
        public string ApiToken = "";
        /// <summary>本地 Facility.ID(源学校)</summary>
        public string SourceFacilityId = "";
        /// <summary>学校名称(用于在本地库建立 Facility 行)</summary>
        public string SchoolName = "";
        /// <summary>保存设置时平台当前项目 ID(每轮上传前比对,变更即停轮提示核对)</summary>
        public string RemoteProjectId = "";
        /// <summary>保存设置时平台当前项目名称(仅用于提示文案)</summary>
        public string RemoteProjectName = "";

        public static SyncConfig Load()
        {
            var conf = new SyncConfig();
            var result = ProviderFactory.Create<IProvider<SyncConfigEntry, string>>(LocalDb.DalConnStr).GetItems(null);
            if (result.Result != ResultCode.Successful) throw new ConfigException("读取同步配置失败: " + result.Message);
            foreach (var entry in result.QueryObjects)
            {
                switch (entry.ID)
                {
                    case "api_url": conf.ApiUrl = entry.Value; break;
                    case "api_token": conf.ApiToken = entry.Value; break;
                    case "source_facility_id": conf.SourceFacilityId = entry.Value; break;
                    case "school_name": conf.SchoolName = entry.Value; break;
                    case "remote_project_id": conf.RemoteProjectId = entry.Value; break;
                    case "remote_project_name": conf.RemoteProjectName = entry.Value; break;
                }
            }
            conf.ApiUrl = conf.ApiUrl.Trim().TrimEnd('/');
            return conf;
        }





        public void Save()
        {
            ProviderFactory.Create<SyncConfigProvider>(LocalDb.DalConnStr).SavePairs(new Dictionary<string, string>
            {
                { "api_url", ApiUrl }, { "api_token", ApiToken },
                { "source_facility_id", SourceFacilityId }, { "school_name", SchoolName },
                { "remote_project_id", RemoteProjectId }, { "remote_project_name", RemoteProjectName }
            });
        }

        /// <summary>仅校验接口地址与令牌格式(保存设置时、还未取得学校绑定前使用)。</summary>
        public void ValidateUrlToken()
        {
            if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrWhiteSpace(ApiToken))
                throw new ConfigException("缺少必填配置:接口地址 / 令牌");
            Uri uri;
            if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrEmpty(uri.Host))
                throw new ConfigException("同步接口地址必须是有效的 HTTP(S) 地址");
            if (!ApiUrl.EndsWith("/api/hh", StringComparison.OrdinalIgnoreCase))
                throw new ConfigException("同步接口地址必须以 /api/hh 结尾");
        }

        /// <summary>
        /// 校验配置是否足够发起一次同步(含学校绑定,任务发起前使用)。
        /// </summary>
        public void Validate()
        {
            ValidateUrlToken();
            if (string.IsNullOrWhiteSpace(SourceFacilityId))
                throw new ConfigException("缺少学校绑定,请先到同步设置中保存一次(保存时会自动从平台读取学校)");
        }

        /// <summary>
        /// 确保当前项目下存在指定名称的 Facility 行,返回其 ID。
        /// 已按名称存在则复用;不存在则新建(ID 用 Guid)。
        /// </summary>
        public static string EnsureFacility(string schoolName, string projectID)
        {
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ID FROM Facility WHERE ProjectID = @p AND Name = @n LIMIT 1";
                    cmd.Parameters.AddWithValue("@p", projectID);
                    cmd.Parameters.AddWithValue("@n", schoolName);
                    object found = cmd.ExecuteScalar();
                    if (found != null) return Convert.ToString(found);
                }
                string id = Guid.NewGuid().ToString();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Facility (ID, Name, ProjectID) VALUES (@id, @n, @p)";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@n", schoolName);
                    cmd.Parameters.AddWithValue("@p", projectID);
                    cmd.ExecuteNonQuery();
                }
                return id;
            }
        }

        /// <summary>
        /// 确保学校绑定落在当前项目下:source_facility_id 指向的 Facility 不属于当前项目时
        /// (跨学年/切换项目后,学生若写进旧项目则新项目永远看不到),按学校名在当前项目下
        /// 查找或重建 Facility 并更新绑定。绑定发生变化的返回描述文本(供记日志),
        /// 未变化或未绑定过返回 null。
        /// </summary>
        public static string RebindFacilityForProject(string projectID)
        {
            var conf = Load();
            if (string.IsNullOrWhiteSpace(conf.SourceFacilityId) || string.IsNullOrWhiteSpace(conf.SchoolName))
                return null;
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ProjectID FROM Facility WHERE ID = @id LIMIT 1";
                    cmd.Parameters.AddWithValue("@id", conf.SourceFacilityId);
                    object row = cmd.ExecuteScalar();
                    if (row != null && Convert.ToString(row) == projectID)
                        return null; // 绑定已在当前项目下
                }
            }
            conf.SourceFacilityId = EnsureFacility(conf.SchoolName, projectID);
            conf.Save();
            return "学校绑定已跟随当前项目重建:" + conf.SchoolName;
        }
    }

    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }
}
