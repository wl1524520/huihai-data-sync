using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>每个安装路径独立的设备身份，与业务数据库及硬件无关。</summary>
    public static class DeviceIdentity
    {
        public static string IdentityPath
        {
            get
            {
                string installation;
                using (var hash = SHA256.Create())
                    installation = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(
                        Path.GetFullPath(Application.StartupPath).TrimEnd('\\').ToUpperInvariant()))).Replace("-", "");
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Huihai", "DataCollection", installation, "device-id.txt");
            }
        }

        public static string LoadOrCreate()
        {
            return LoadOrCreate(IdentityPath);
        }

        internal static string LoadOrCreate(string path)
        {
            // CreateNew 保证并发创建不会覆盖既有身份。文件存在但损坏时绝不重新生成。
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path))
            {
                try
                {
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                        writer.Write(Guid.NewGuid().ToString("D"));
                }
                catch (IOException) { if (!File.Exists(path)) throw; }
            }
            string value = File.ReadAllText(path, Encoding.UTF8);
            Guid id;
            if (!Guid.TryParseExact(value, "D", out id) || id == Guid.Empty)
                throw new ConfigException("设备身份文件损坏，请恢复原文件：" + path);
            return value;
        }
    }
}
