using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using HH.DataCollection.Wifi.Db;
using HH.DataCollection.Wifi.Sync;
using HH.TiYu.Model;
using Newtonsoft.Json.Linq;

internal static class VerifySyncProtocol
{
    private static int checks;
    private static readonly string Device = "550e8400-e29b-41d4-a716-446655440000";
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Fails<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T ex) { Check(ex.Message.Contains(message), "Wrong failure detail: " + ex.Message); return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static void Sql(string sql)
    {
        using (var conn = new SQLiteConnection(LocalDb.ConnStr))
        {
            conn.Open();
            using (var cmd = new SQLiteCommand(sql, conn)) cmd.ExecuteNonQuery();
        }
    }
    private sealed class Request
    {
        public string Line;
        public string Headers;
        public string Body;
    }
    private sealed class Reply
    {
        public int Status = 200;
        public string Body;
        public string Extra = "";
    }
    private sealed class Server : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly Thread worker;
        private volatile bool closing;
        private Exception error;
        public readonly string Url;
        public readonly List<Request> Requests = new List<Request>();
        public Server(Func<Request, int, Reply> reply)
        {
            listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/api/hh";
            worker = new Thread(delegate()
            {
                try
                {
                    while (!closing)
                    using (var client = listener.AcceptTcpClient())
                    using (var stream = client.GetStream())
                    {
                        client.ReceiveTimeout = 5000;
                        var head = new StringBuilder();
                        while (!head.ToString().EndsWith("\r\n\r\n"))
                        {
                            int value = stream.ReadByte();
                            if (value < 0) throw new IOException("Incomplete header");
                            head.Append((char)value);
                        }
                        int length = 0;
                        foreach (var line in head.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None))
                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                                length = int.Parse(line.Substring(15).Trim());
                        if (head.ToString().IndexOf("Expect: 100-continue", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            byte[] interim = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
                            stream.Write(interim, 0, interim.Length);
                        }
                        byte[] bytes = new byte[length];
                        for (int offset = 0; offset < length; )
                        {
                            int count = stream.Read(bytes, offset, length - offset);
                            if (count == 0) throw new IOException("Incomplete body");
                            offset += count;
                        }
                        var request = new Request { Line = head.ToString().Split('\r')[0], Headers = head.ToString(), Body = Encoding.UTF8.GetString(bytes) };
                        Requests.Add(request);
                        var response = reply(request, Requests.Count);
                        byte[] data = Encoding.UTF8.GetBytes(response.Body);
                        byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 " + response.Status + " Fixture\r\nContent-Type: application/json\r\nContent-Length: " +
                            data.Length + "\r\nConnection: close\r\n" + response.Extra + "\r\n");
                        stream.Write(header, 0, header.Length);
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception ex) { if (!closing) error = ex; }
            }) { IsBackground = true };
            worker.Start();
        }
        public void Dispose()
        {
            closing = true;
            listener.Stop();
            Check(worker.Join(3000) && error == null, "Fixture error: " + error);
        }
    }
    private static string Identity(string path)
    {
        try
        {
            return (string)typeof(DeviceIdentity).GetMethod("LoadOrCreate", BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(string) }, null).Invoke(null, new object[] { path });
        }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static JArray Items()
    {
        return JArray.Parse("[{test_no:'000aBc',code:10001,score:'170.50',completed_at:'2026-10-25 10:20:30'}]");
    }
    private static void VerifyIdentity()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "identity", "device-id.txt");
        string number = Identity(path);
        Guid value;
        Check(number.Length == 36 && Guid.TryParseExact(number, "D", out value), "Device must be a bare GUID");
        Check(Identity(path) == number, "Restart changed device identity");
        Sql("DELETE FROM SyncConfig"); // Simulate a recreated business configuration.
        Check(Identity(path) == number, "Database reset changed identity");
        Check(Identity(Path.Combine(Path.GetDirectoryName(path), "second.txt")) != number, "Distinct installs shared identity");
        File.WriteAllText(path, "broken");
        Fails<ConfigException>(() => Identity(path), "损坏");
        Check(File.ReadAllText(path) == "broken", "Corrupt identity was overwritten");
    }
    private static void VerifyRequests()
    {
        foreach (var state in new[] { "pending_authorization", "pending_school", "active" })
        using (var server = new Server((r, n) => new Reply { Body = "{\"status\":\"" + state + "\",\"message\":\"状态\"}" }))
        {
            var api = new SyncApi(server.Url, Device);
            Check(api.Register().Status == state && api.Register().Status == state, "Registration state/idempotent call");
            Check(server.Requests.Count == 2 && server.Requests[0].Body == server.Requests[1].Body, "Registration changed identity");
            var request = server.Requests[0];
            var json = JObject.Parse(request.Body);
            Check(json.Count == 1 && (string)json["device_no"] == Device && request.Line.StartsWith("POST /api/hh/sync/register "), "Registration whitelist");
            Check(!request.Headers.Contains("X-Huihai-Token") && request.Headers.Contains("application/json"), "Legacy header present");
        }
        using (var server = new Server((r, n) => new Reply { Body = "{status:1,message:'bad'}" }))
            Fails<SyncException>(() => new SyncApi(server.Url, Device).Register(), "格式错误");
        using (var server = new Server((r, n) => new Reply { Body = r.Line.Contains("/info?")
            ? "{school:{id:1,name:'School'},project:{id:2,name:'Project'}}"
            : "{items:[],meta:{current_page:1,per_page:1000,last_page:1,total:0,project_id:2,project_year:2026}}" }))
        {
            var api = new SyncApi(server.Url, Device);
            Check(api.GetSyncInfo(false).ProjectId == "2", "Info response");
            Check(api.GetStudentPage(1, 1000).Total == 0, "Empty roster response");
            Check(server.Requests[0].Line == "GET /api/hh/sync/info?device_no=" + Device + " HTTP/1.1", "Info query whitelist");
            Check(server.Requests[1].Line == "GET /api/hh/sync/student?device_no=" + Device + "&page=1&per_page=1000 HTTP/1.1", "Student query whitelist");
        }
        foreach (var endpoint in new[] { "register", "info", "student", "score" })
        using (var server = new Server((r, n) => new Reply { Status = 403, Body = "{message:'disabled'}" }))
        {
            var api = new SyncApi(server.Url, Device);
            Fails<SyncAuthException>(() => { if (endpoint == "register") api.Register(); else if (endpoint == "info") api.GetSyncInfo(true);
                else if (endpoint == "student") api.GetStudentPage(1, 1000); else api.PostScores(Items()); }, "403");
            Check(server.Requests.Count == 1, "403 was retried");
        }
        foreach (var response in new[] { "{success:true}", "{success:false}", "{success:'true'}", "[]", "not-json" })
        using (var server = new Server((r, n) => new Reply { Body = response }))
        {
            var api = new SyncApi(server.Url, Device);
            if (response == "{success:true}") api.PostScores(Items());
            else Fails<SyncException>(() => api.PostScores(Items()), "");
            var body = JObject.Parse(server.Requests[0].Body);
            Check(body.Count == 2 && (string)body["device_no"] == Device && ((JArray)body["items"]).Count == 1, "Score top-level whitelist");
            Check(((JObject)body["items"][0]).Count == 4 && (string)body["items"][0]["test_no"] == "000aBc", "Score row whitelist/case preservation");
            Check(server.Requests.Count == 1, "Invalid ACK retried automatically");
        }
        foreach (int failure in new[] { 429, 503 })
        using (var server = new Server((r, n) => new Reply { Status = n == 1 ? failure : 200, Extra = "Retry-After: 0\r\n", Body = "{success:true}" }))
        {
            new SyncApi(server.Url, Device).PostScores(Items());
            Check(server.Requests.Count == 2 && server.Requests[0].Body == server.Requests[1].Body, "Retry changed original payload");
        }
        using (var server = new Server((r, n) => new Reply { Status = 429, Extra = "Retry-After: 0\r\n", Body = "{}" }))
        {
            Fails<SyncRateLimitedException>(() => new SyncApi(server.Url, Device).PostScores(Items()), "429");
            Check(server.Requests.Count == 3, "Unbounded rate limit retry");
        }
        using (var server = new Server((r, n) => new Reply { Status = 422, Body = "{errors:{'items.0.test_no':['too long']}}" }))
            Fails<SyncBatchRejectedException>(() => new SyncApi(server.Url, Device).PostScores(Items()), "items.0.test_no");
        foreach (var body in new[] { "{errors:{device_no:['invalid']}}", "{data_issues:{total:123,issue_counts:{duplicate_student_no:123},issues:[]}}" })
        using (var server = new Server((r, n) => new Reply { Status = 422, Body = body }))
            Fails<SyncException>(() => new SyncApi(server.Url, Device).GetStudentPage(1, 1000), body.Contains("errors") ? "device_no" : "123");
    }
    private static void VerifyTargetsAndCase()
    {
        Sql("DELETE FROM SyncConfig");
        var config = new SyncConfig { ApiUrl = "http://example.invalid/api/hh", DeviceNo = Device, RegistrationStatus = "active", SchoolId = "1", RemoteProjectId = "2" };
        config.Save();
        var target = new SyncInfo { SchoolId = "1", ProjectId = "2" };
        config.BindTarget(target, "protocol");
        Sql("INSERT INTO Student(ID,StudentID,ProjectID,Name,Sex,Grade) VALUES('protocol','abc','protocol','Student',1,11)");
        config.ValidateTarget(target, "protocol");
        Fails<SyncFatalException>(() => config.ValidateTarget(new SyncInfo { SchoolId = "9", ProjectId = "2" }, "protocol"), "学校");
        Fails<ConfigException>(() => config.BindTarget(new SyncInfo { SchoolId = "9", ProjectId = "2" }, "empty-year"), "数据");
        Fails<ConfigException>(() => config.BindTarget(new SyncInfo { SchoolId = "1", ProjectId = "3" }, "protocol"), "数据");
        config.BindTarget(new SyncInfo { SchoolId = "1", ProjectId = "3" }, "empty-year");
        var provider = HH.TiYu.DAL.ProviderFactory.Create<HH.TiYu.DAL.LinqProvider.StudentProvider>(LocalDb.DalConnStr);
        provider.SyncRemotePage(new List<Student> { new Student { StudentID = "ABC", Name = "Other", Sex = Sex.Male, Grade = 11 } }, "protocol");
        var project = new PhysicalProject();
        project.AddStudents(new List<Student> { new Student { StudentID = "abc", Name = "Lower" }, new Student { StudentID = "ABC", Name = "Upper" } });
        Check(project.AllStudents.Count == 2 && project.GetStudentByID("abc").Name == "Lower" && project.GetStudentByID("ABC").Name == "Upper", "In-memory identifiers folded case");
        Check(provider.GetItems(new HH.TiYu.Model.SearchCondition.StudentSearchCondition { ProjectID = "protocol" }).QueryObjects.Count == 2, "DAL identifiers folded case");
        provider.SyncRemotePage(new List<Student> { new Student { StudentID = new string('0', 60), Name = "Long", Sex = Sex.Male } }, "protocol");
        Fails<ArgumentException>(() => provider.SyncRemotePage(new List<Student> { new Student { StudentID = new string('0', 61) } }, "protocol"), "60");
        Sql("DELETE FROM Student WHERE ProjectID='protocol'");
        Sql("DELETE FROM SyncConfig");
    }

    private static void VerifyStudentFields()
    {
        var previous = PhysicalProject.Current;
        PhysicalProject.Current = new PhysicalProject { ID = "protocol-fields" };
        try
        {
            var sync = new StudentSync(null, new SyncConfig());
            var upsert = typeof(StudentSync).GetMethod("UpsertPage", BindingFlags.NonPublic | BindingFlags.Instance);
            Action<JArray, List<string>> save = (items, issues) =>
            {
                try { upsert.Invoke(sync, new object[] { items, "protocol-fields", issues }); }
                catch (TargetInvocationException ex) { throw ex.InnerException; }
            };
            var valid = JArray.Parse("[{test_no:'0000aBc',name:'Student',gender:1,grade:11,class_name:''}]");
            valid[0]["test_no"] = new string('0', 60);
            valid[0]["name"] = new string('名', 60);
            valid[0]["class_name"] = new string('班', 100);
            save(valid, new List<string>());
            Check(true, "Maximum valid text lengths must be accepted");
            foreach (var field in new[] { "test_no", "name", "class_name", "gender", "grade" })
            {
                var page = JArray.Parse("[{test_no:'must-not-commit',name:'Good',gender:1,grade:11,class_name:'Class'},{test_no:'bad',name:'Bad',gender:1,grade:11,class_name:'Class'}]");
                page[1][field] = field == "gender" || field == "grade" ? (JToken)"1" : JValue.CreateNull();
                Fails<SyncException>(() => save(page, new List<string>()), "服务端学生");
            }
            var provider = HH.TiYu.DAL.ProviderFactory.Create<HH.TiYu.DAL.LinqProvider.StudentProvider>(LocalDb.DalConnStr);
            Check(provider.GetItems(new HH.TiYu.Model.SearchCondition.StudentSearchCondition { ProjectID = "protocol-fields" }).QueryObjects.Count == 1,
                "Invalid page partially committed");
            foreach (int grade in new[] { 17, 20, 24, 30, 34, 40, 45 })
            {
                var page = JArray.Parse("[{test_no:'grade-hole',name:'Student',gender:1,grade:11,class_name:'Class'}]");
                page[0]["grade"] = grade;
                var issues = new List<string>();
                save(page, issues);
                Check(issues.Count == 1, "Gap in grade codes was accepted: " + grade);
            }
        }
        finally
        {
            PhysicalProject.Current = previous;
            Sql("DELETE FROM Student WHERE ProjectID='protocol-fields'");
        }
    }
    public static int Run()
    {
        VerifyIdentity();
        VerifyRequests();
        VerifyTargetsAndCase();
        VerifyStudentFields();
        Console.WriteLine("Device protocol verification passed: " + checks + " checks.");
        return checks;
    }
}
