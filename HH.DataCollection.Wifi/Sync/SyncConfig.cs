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
        public string ApiToken = "";
        /// <summary>令牌对应的学校名称(仅用于显示)</summary>
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
                { "school_name", SchoolName },
                { "remote_project_id", RemoteProjectId }, { "remote_project_name", RemoteProjectName }
            });
        }

        /// <summary>仅校验接口地址与令牌格式(保存设置和任务发起前使用)。</summary>
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
        /// 校验配置是否足够发起一次同步(地址与令牌,任务发起前使用)。
        /// </summary>
        public void Validate()
        {
            ValidateUrlToken();
        }

    }

    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }
}
