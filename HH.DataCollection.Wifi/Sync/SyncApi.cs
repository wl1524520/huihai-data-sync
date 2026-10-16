using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 同步接口的 HTTP 层(.NET 4.0 无 HttpClient,用 HttpWebRequest)。
    /// 契约见 unite-tizhi-docs/docs/汇海同步接口文档.md(2026-10-16 版):
    ///   认证头 X-Huihai-Token;base 必须以 /api/hh 结尾;
    ///   成功判定严格为 HTTP 200 + JSON + success===true;
    ///   429 遵从 Retry-After 退避,5xx/网络错误有上限退避重试,重试保留原载荷(§7)。
    /// </summary>
    public class SyncApi
    {
        private static readonly int[] RetryableStatusCodes = { 429, 500, 502, 503, 504 };
        private const int MaxAttempts = 3;
        private const int TimeoutMs = 30000;
        /// <summary>429 未带 Retry-After 时的默认退避(毫秒)</summary>
        private const int DefaultRateLimitDelayMs = 30000;

        private readonly string _baseUrl;
        private readonly string _token;

        public SyncApi(string baseUrl, string token)
        {
            _baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
            _token = token ?? "";
        }

        /// <summary>GET /sync/info:返回 token 绑定的学校与平台当前项目。retry=true 时网络错误/限流/5xx 重试至多 3 次(间隔 1s/2s)。</summary>
        public SyncInfo GetSyncInfo(bool retry)
        {
            int attempts = retry ? MaxAttempts : 1;
            string lastError = null;
            for (int i = 0; i < attempts; i++)
            {
                HttpResult resp = null;
                try
                {
                    resp = Send("GET", "/sync/info", null, null);
                }
                catch (Exception ex)
                {
                    lastError = "网络请求失败:" + ex.Message;
                }
                if (resp != null)
                {
                    if (resp.StatusCode == 200)
                    {
                        var data = ParseJsonObject(resp.Body, "服务端返回的同步信息不是有效 JSON");
                        var school = data["school"] as JObject;
                        var project = data["project"] as JObject;
                        if (school == null || project == null ||
                            school["id"] == null || project["id"] == null ||
                            school["name"] == null || project["name"] == null)
                            throw new SyncException("服务端返回的同步信息格式错误");
                        return new SyncInfo
                        {
                            SchoolId = school["id"].ToString(),
                            SchoolName = school["name"].ToString(),
                            ProjectId = project["id"].ToString(),
                            ProjectName = project["name"].ToString(),
                        };
                    }
                    lastError = "HTTP " + resp.StatusCode;
                    if (Array.IndexOf(RetryableStatusCodes, resp.StatusCode) < 0)
                        throw new SyncException(lastError);
                    if (resp.StatusCode == 429 && i < attempts - 1)
                    {
                        // 429 遵从 Retry-After(秒),缺失或非数字时退避 30s(文档 §7)
                        System.Threading.Thread.Sleep(
                            resp.RetryAfterSeconds.HasValue ? resp.RetryAfterSeconds.Value * 1000 : DefaultRateLimitDelayMs);
                        continue;
                    }
                }
                if (i < attempts - 1) System.Threading.Thread.Sleep(1000 << i);
            }
            throw new SyncException(lastError ?? "获取同步信息失败");
        }

        /// <summary>
        /// GET /sync/student?page=N:拉一页学生。网络错误/429/5xx 重试至多 3 次(间隔 1s/2s,与 GetSyncInfo 一致);
        /// 422 区分 errors(分页参数错误)与 data_issues(全校脏数据,文档 §7:不能把所有 422 都当脏数据)。
        /// </summary>
        public StudentPage GetStudentPage(int page, int perPage)
        {
            string lastError = null;
            for (int i = 0; i < MaxAttempts; i++)
            {
                HttpResult resp = null;
                try
                {
                    resp = Send("GET", "/sync/student", "page=" + page + "&per_page=" + perPage, null);
                }
                catch (Exception ex)
                {
                    lastError = "网络请求失败:" + ex.Message;
                }
                if (resp != null)
                {
                    if (resp.StatusCode == 200)
                    {
                        var data = ParseJsonObject(resp.Body, "服务端学生分页响应不是有效 JSON");
                        return ValidateStudentPage(data, page);
                    }
                    if (resp.StatusCode == 422)
                        throw BuildStudentPage422(resp.Body); // 参数错/脏数据都不重试
                    lastError = "HTTP " + resp.StatusCode;
                    if (Array.IndexOf(RetryableStatusCodes, resp.StatusCode) < 0)
                        throw new SyncException(lastError);
                }
                if (i < MaxAttempts - 1) System.Threading.Thread.Sleep(1000 << i);
            }
            throw new SyncException(lastError ?? "获取学生名单失败");
        }

        /// <summary>学生分页 422:errors 结构是参数验证失败,data_issues 才是全校脏数据。</summary>
        private static SyncException BuildStudentPage422(string body)
        {
            string message = null;
            try
            {
                var err = JObject.Parse(body);
                if (err["message"] != null) message = err["message"].ToString();
                var errors = err["errors"] as JObject;
                if (errors != null && errors.HasValues)
                {
                    string detail = FlattenErrors(errors);
                    return new SyncException("学生同步请求参数错误(422)" +
                        (message != null ? ":" + message : "") +
                        (detail.Length > 0 ? " [" + detail + "]" : ""));
                }
            }
            catch (SyncException) { throw; }
            catch { /* 落到默认脏数据提示 */ }
            var sb = new StringBuilder(message ?? "平台学生数据存在脏数据,请先处理后再同步");
            try
            {
                var issues = JObject.Parse(body)["data_issues"] as JObject;
                var counts = issues != null ? issues["issue_counts"] as JObject : null;
                if (counts != null && counts.HasValues)
                {
                    var parts = new StringBuilder();
                    foreach (var kv in counts)
                        parts.Append(parts.Length == 0 ? "(" : ";").Append(kv.Key).Append(":").Append(kv.Value);
                    sb.Append(parts.Append(")"));
                }
            }
            catch { /* 用已拼好的提示 */ }
            return new SyncException(sb.ToString());
        }

        /// <summary>
        /// POST /sync/score:上传一批成绩(1~5000 条),重试保留原载荷(文档 §7)。
        /// 仅当 HTTP 200 + JSON + success===true 视为平台已接收。
        /// 异常分类(依据 2026-10-16 版接口文档 §4/§7):
        ///   401 → SyncAuthException(停止本轮);400 → SyncFatalException(停止本轮);
        ///   200+success=false → SyncBatchRejectedException(部分明细保存失败,停止本轮,重启后原样补传);
        ///   422 → SyncBatchRejectedException(字段验证失败,附 errors 字段定位,修复前重发无意义,停止本轮);
        ///   429 → 按 Retry-After(无则 30s)退避重试,连续 3 次未缓解抛 SyncRateLimitedException(停止本轮);
        ///   5xx/网络错误 → 至多 3 次指数退避(1s/2s)后抛 SyncException(调用方跳过本批继续下一批)。
        /// </summary>
        public void PostScores(JArray items)
        {
            var payload = new JObject();
            payload["items"] = items;
            string body = payload.ToString(Newtonsoft.Json.Formatting.None);
            string lastError = null;
            for (int i = 0; i < MaxAttempts; i++)
            {
                HttpResult resp = null;
                try
                {
                    resp = Send("POST", "/sync/score", null, body);
                }
                catch (Exception ex)
                {
                    lastError = "网络请求失败:" + ex.Message;
                }
                if (resp != null)
                {
                    if (resp.StatusCode == 200)
                    {
                        var data = ParseJsonObject(resp.Body, "服务端返回的成功响应不是有效 JSON");
                        var success = data["success"];
                        if (success != null && success.Type == JTokenType.Boolean)
                        {
                            if (success.Value<bool>()) return;
                            // success=false:至少一条原始明细保存失败(已保存行平台保留),
                            // 本批不得标记,提示错误并停止本轮,重新启动后原样补传
                            throw new SyncBatchRejectedException(
                                "部分原始成绩保存失败:" + (data["message"] != null ? data["message"].ToString() : "请重新上传"));
                        }
                        throw new SyncException(data["message"] != null ? data["message"].ToString() : "服务端响应不满足接收协议");
                    }
                    if (resp.StatusCode == 401)
                        throw new SyncAuthException("令牌无效或已停用(401),请检查同步设置");
                    if (resp.StatusCode == 400)
                        throw new SyncFatalException("学校或项目不存在(400),请核对同步设置与平台配置");
                    if (resp.StatusCode == 422)
                    {
                        string detail = "";
                        try
                        {
                            var err = JObject.Parse(resp.Body);
                            detail = err["message"] != null ? err["message"].ToString() : "";
                            var errors = err["errors"] as JObject;
                            if (errors != null && errors.HasValues)
                                detail += (detail.Length > 0 ? " " : "") + "[" + FlattenErrors(errors) + "]";
                        }
                        catch { }
                        throw new SyncBatchRejectedException("字段验证失败(422)" + (detail.Length > 0 ? ":" + detail : ""));
                    }
                    if (resp.StatusCode == 429)
                    {
                        if (i < MaxAttempts - 1)
                        {
                            // 遵从 Retry-After(秒);缺失或非数字时退避 30s(文档 §7)
                            int delayMs = resp.RetryAfterSeconds.HasValue ? resp.RetryAfterSeconds.Value * 1000 : DefaultRateLimitDelayMs;
                            System.Threading.Thread.Sleep(delayMs);
                            continue;
                        }
                        throw new SyncRateLimitedException(
                            "平台限流(429),连续 " + MaxAttempts + " 次退避后仍未缓解,停止本轮,下轮原样重传");
                    }
                    lastError = "HTTP " + resp.StatusCode;
                    if (Array.IndexOf(RetryableStatusCodes, resp.StatusCode) < 0)
                        throw new SyncException(lastError); // 404/405 等:按配置排查,不重试
                }
                if (i < MaxAttempts - 1) System.Threading.Thread.Sleep(1000 << i);
            }
            throw new SyncException(lastError ?? "上传成绩失败");
        }

        /// <summary>把 422 的 errors 对象拍平成 "字段:原因;..."(截断保护,避免超大响应刷屏)。</summary>
        private static string FlattenErrors(JObject errors)
        {
            var sb = new StringBuilder();
            foreach (var kv in errors)
            {
                sb.Append(sb.Length == 0 ? "" : "; ").Append(kv.Key);
                var arr = kv.Value as JArray;
                if (arr != null && arr.Count > 0)
                    sb.Append(":").Append(string.Join(",", arr));
                else if (kv.Value != null)
                    sb.Append(":").Append(kv.Value.ToString());
                if (sb.Length > 500) { sb.Append("..."); break; }
            }
            return sb.ToString();
        }

        private static StudentPage ValidateStudentPage(JObject data, int expectedPage)
        {
            var items = data["items"] as JArray;
            var meta = data["meta"] as JObject;
            if (items == null || meta == null)
                throw new SyncException("服务端学生分页响应格式错误:items 或 meta 缺失");
            string[] requiredMeta = { "current_page", "per_page", "last_page", "total", "project_id", "project_year" };
            foreach (var key in requiredMeta)
            {
                var v = meta[key];
                if (v == null || v.Type != JTokenType.Integer)
                    throw new SyncException("服务端学生分页响应格式错误:meta 字段不完整");
            }
            var page = new StudentPage
            {
                Items = items,
                CurrentPage = meta["current_page"].Value<int>(),
                PerPage = meta["per_page"].Value<int>(),
                LastPage = meta["last_page"].Value<int>(),
                Total = meta["total"].Value<int>(),
                ProjectId = meta["project_id"].Value<int>(),
                ProjectYear = meta["project_year"].Value<int>(),
            };
            if (page.CurrentPage != expectedPage || page.CurrentPage < 1 || page.LastPage < 1)
                throw new SyncException("服务端学生分页响应格式错误:页码不一致");
            if (page.CurrentPage > page.LastPage || page.PerPage < 1 || page.Total < 0)
                throw new SyncException("服务端学生分页响应格式错误:分页参数无效");
            // 2026-10-16 12:42 版契约(§3.2/§10):items[] 仅五字段,test_no 取代 student_no 与 id_number
            // (test_no 是学校 qrcode_field 决定的"学号或身份证",客户端不区分、原样处理)
            string[] requiredItemFields = { "test_no", "name", "gender", "grade", "class_name" };
            foreach (var item in items)
            {
                var obj = item as JObject;
                if (obj == null) throw new SyncException("服务端学生分页响应格式错误:学生字段不完整");
                foreach (var f in requiredItemFields)
                    if (obj.Property(f) == null)
                        throw new SyncException("服务端学生分页响应格式错误:学生字段不完整");
            }
            return page;
        }

        private static JObject ParseJsonObject(string body, string errorMessage)
        {
            try
            {
                var obj = JObject.Parse(body);
                return obj;
            }
            catch (Exception)
            {
                throw new SyncException(errorMessage);
            }
        }

        private HttpResult Send(string method, string path, string query, string jsonBody)
        {
            string url = _baseUrl + path + (string.IsNullOrEmpty(query) ? "" : "?" + query);
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.Timeout = TimeoutMs;
            req.ReadWriteTimeout = TimeoutMs;
            req.Accept = "application/json";
            req.Headers["X-Huihai-Token"] = _token;
            if (jsonBody != null)
            {
                req.ContentType = "application/json";
                byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
                req.ContentLength = bytes.Length;
                using (var stream = req.GetRequestStream())
                    stream.Write(bytes, 0, bytes.Length);
            }
            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return new HttpResult { StatusCode = (int)resp.StatusCode, Body = reader.ReadToEnd(), RetryAfterSeconds = ParseRetryAfter(resp) };
            }
            catch (WebException ex)
            {
                var errorResp = ex.Response as HttpWebResponse;
                if (errorResp == null) throw; // 真网络错误,交给调用方
                using (errorResp)
                using (var reader = new StreamReader(errorResp.GetResponseStream(), Encoding.UTF8))
                    return new HttpResult { StatusCode = (int)errorResp.StatusCode, Body = reader.ReadToEnd(), RetryAfterSeconds = ParseRetryAfter(errorResp) };
            }
        }

        /// <summary>解析 Retry-After 响应头(仅支持秒数形式;HTTP 日期等返回 null)。</summary>
        private static int? ParseRetryAfter(HttpWebResponse resp)
        {
            string value = resp.Headers["Retry-After"];
            int seconds;
            if (!string.IsNullOrEmpty(value) && int.TryParse(value.Trim(), out seconds) && seconds >= 0)
                return seconds;
            return null;
        }

        private class HttpResult
        {
            public int StatusCode;
            public string Body;
            public int? RetryAfterSeconds;
        }
    }

    public class SyncInfo
    {
        public string SchoolId;
        public string SchoolName;
        public string ProjectId;
        public string ProjectName;

        public override string ToString()
        {
            return string.Format("远端同步信息:学校:{0}(ID:{1});项目:{2}(ID:{3})", SchoolName, SchoolId, ProjectName, ProjectId);
        }
    }

    public class StudentPage
    {
        public JArray Items;
        public int CurrentPage;
        public int PerPage;
        public int LastPage;
        public int Total;
        public int ProjectId;
        public int ProjectYear;
    }

    /// <summary>普通同步错误:当前批次/操作失败,但不影响后续批次。</summary>
    public class SyncException : Exception
    {
        public SyncException(string message) : base(message) { }
    }

    /// <summary>
    /// 本批数据被服务端拒收(200+success=false 部分保存失败,或 422 字段验证失败):
    /// 不得标记本批,停止本轮;修复或重启后原样补传(文档 §6.2/§7)。
    /// </summary>
    public class SyncBatchRejectedException : SyncException
    {
        public SyncBatchRejectedException(string message) : base(message) { }
    }

    /// <summary>令牌失效:停止本轮,不继续后续批次。</summary>
    public class SyncAuthException : SyncException
    {
        public SyncAuthException(string message) : base(message) { }
    }

    /// <summary>学校/项目不存在等平台侧配置错误:停止本轮。</summary>
    public class SyncFatalException : SyncException
    {
        public SyncFatalException(string message) : base(message) { }
    }

    /// <summary>平台限流(429)经退避重试仍未缓解:停止本轮,下轮原样重传。</summary>
    public class SyncRateLimitedException : SyncException
    {
        public SyncRateLimitedException(string message) : base(message) { }
    }
}
