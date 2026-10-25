using System;
using System.Collections.Generic;
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
        // 独立身份文件的缓存；设备号只由 DeviceIdentity 生成。
        public string DeviceNo = "";
        public string RegistrationStatus = "";
        public string SchoolId = "";
        /// <summary>设备绑定的学校名称(仅用于显示)</summary>
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
                    case "device_no": conf.DeviceNo = entry.Value; break;
                    case "registration_status": conf.RegistrationStatus = entry.Value; break;
                    case "school_id": conf.SchoolId = entry.Value; break;
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
                { "api_url", ApiUrl }, { "device_no", DeviceNo },
                { "registration_status", RegistrationStatus }, { "school_id", SchoolId },
                { "school_name", SchoolName },
                { "remote_project_id", RemoteProjectId }, { "remote_project_name", RemoteProjectName }
            });
        }

        /// <summary>注册前只校验地址与设备号，允许尚未授权。</summary>
        public void ValidateConnection()
        {
            if (string.IsNullOrWhiteSpace(ApiUrl))
                throw new ConfigException("请配置同步接口地址");
            if (DeviceNo == null || DeviceNo.Length < 10 || DeviceNo.Length > 100 || DeviceNo != DeviceNo.Trim())
                throw new ConfigException("设备号须为 10 至 100 个字符且不含首尾空白");
            Uri uri;
            if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrEmpty(uri.Host))
                throw new ConfigException("同步接口地址必须是有效的 HTTP(S) 地址");
            if (!ApiUrl.EndsWith("/api/hh", StringComparison.OrdinalIgnoreCase))
                throw new ConfigException("同步接口地址必须以 /api/hh 结尾");
        }

        /// <summary>
        /// 校验是否已确认设备授权和同步目标；每次请求仍由平台实时鉴权。
        /// </summary>
        public void Validate()
        {
            ValidateConnection();
            if (RegistrationStatus != "active" || string.IsNullOrEmpty(SchoolId) || string.IsNullOrEmpty(RemoteProjectId))
                throw new ConfigException("请在同步设置中检查设备授权并确认学校及当前项目");
        }

        public void ValidateTarget(SyncInfo info, string projectId)
        {
            Validate();
            if (SchoolId != info.SchoolId)
                throw new SyncFatalException("设备绑定学校已切换，停止同步；请归档并清理原学校数据后重新检查授权");
            if (RemoteProjectId != info.ProjectId)
                throw new SyncFatalException("平台当前项目已切换，停止同步；请核对本地年度及原始数据后重新检查授权");
            BindTarget(info, projectId);
        }

        /// <summary>将本地年度固定到平台地址、学校和项目。有数据时禁止更换目标。</summary>
        public void BindTarget(SyncInfo info, string projectId)
        {
            if (string.IsNullOrEmpty(projectId)) throw new ConfigException("未选择本地年度项目");
            var provider = ProviderFactory.Create<SyncConfigProvider>(LocalDb.DalConnStr);
            string key = "local_target_" + projectId;
            string target = ApiUrl.Trim().TrimEnd('/') + "|" + info.SchoolId + "|" + info.ProjectId;
            string previous = null;
            var entries = provider.GetItems(null);
            if (entries.Result != ResultCode.Successful) throw new ConfigException("读取本地同步目标失败");
            foreach (var entry in entries.QueryObjects)
                if (entry.ID == key) previous = entry.Value;
            foreach (var entry in entries.QueryObjects)
                if (entry.ID.StartsWith("local_target_", StringComparison.Ordinal) &&
                    !entry.Value.StartsWith(ApiUrl.Trim().TrimEnd('/') + "|" + info.SchoolId + "|", StringComparison.Ordinal) &&
                    ProviderFactory.Create<StudentProvider>(LocalDb.DalConnStr).HasLocalData(entry.ID.Substring("local_target_".Length)))
                    throw new ConfigException("本地仍有其他学校或平台的数据，请归档并清理原数据后再改绑设备。");
            var old = Load();
            bool schoolChanged = !string.IsNullOrEmpty(old.SchoolId) &&
                (old.SchoolId != info.SchoolId || old.ApiUrl != ApiUrl);
            bool hasData = ProviderFactory.Create<StudentProvider>(LocalDb.DalConnStr)
                .HasLocalData(schoolChanged ? null : projectId);
            if (hasData && (schoolChanged || (previous != null && previous != target)))
                throw new ConfigException("本地仍有原同步目标的名单或成绩，不能改绑。请先停止采集和同步，归档并清理原数据，或使用无数据的正确年度项目。");
            provider.SavePairs(new Dictionary<string, string> { { key, target } });
        }

    }

    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }
}
