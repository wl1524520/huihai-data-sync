// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using HH.GeneralLibrary.ExceptionHandling;

namespace HH.TiYu.Model
{
    /// <summary>
    /// 采集器配置。原 Model.dll 的硬件与远程库开关（串口、读卡器、条码枪、语音、
    /// 相机、远程服务器/端口、离线、库升级、记住登录 ID、成绩单模式、用于抽测）
    /// 在本轮精简中移除：采集器固定使用本机 SQLite 单库，这些开关在运行时恒为默认值。
    /// 仅保留 LocalDB / StudentScoreDB / ProjectID 与年级区间。
    /// </summary>
    public class AppSettings
    {
        private static AppSettings _instance = null;

        private XmlDocument _doc = null;

        private XmlNode _parent = null;

        private string _path;

        public static AppSettings Current
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppSettings(Path.Combine(Application.StartupPath, "config.xml"));
                }
                return _instance;
            }
        }

        public string ConnStr
        {
            get
            {
                // 采集器单库策略：不再支持 RemoteDB 覆盖。
                return LocalDB;
            }
        }

        public string LocalDB { get; set; }

        public string StudentScoreDB
        {
            get
            {
                // Collector single-database policy; replaces the former Model.dll build patch.
                return "SQLITE:Data Source=" + Path.Combine(Application.StartupPath, "HH_Data.db");
            }
        }

        public string ProjectID
        {
            get
            {
                return GetConfigContent("PhysicalProject.Current");
            }
            set
            {
                SaveConfig("PhysicalProject.Current", value);
            }
        }

        public int? MinGrade { get; set; }

        public int? MaxGrade { get; set; }

        public AppSettings(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    _path = path;
                    _doc = new XmlDocument();
                    _doc.Load(_path);
                    _parent = _doc.SelectSingleNode("configuration/appSettings");
                }
                catch
                {
                }
            }
        }

        public bool SaveConfig(string configName, string configContent)
        {
            if (_parent != null)
            {
                try
                {
                    XmlElement xmlElement = null;
                    XmlAttribute xmlAttribute = null;
                    XmlAttribute xmlAttribute2 = null;
                    XmlNodeList childNodes = _parent.ChildNodes;
                    foreach (XmlNode item in childNodes)
                    {
                        if (item is XmlElement)
                        {
                            XmlElement xmlElement2 = (XmlElement)item;
                            if (xmlElement2.GetAttribute("key") == configName)
                            {
                                xmlElement2.SetAttribute("value", configContent);
                                xmlElement = xmlElement2;
                                break;
                            }
                        }
                    }
                    if (xmlElement == null)
                    {
                        xmlElement = _doc.CreateElement("add");
                        xmlAttribute = _doc.CreateAttribute("key");
                        xmlAttribute.Value = configName;
                        xmlAttribute2 = _doc.CreateAttribute("value");
                        xmlAttribute2.Value = configContent;
                        xmlElement.Attributes.Append(xmlAttribute);
                        xmlElement.Attributes.Append(xmlAttribute2);
                        _parent.AppendChild(xmlElement);
                    }
                    _doc.Save(_path.ToString());
                    return true;
                }
                catch (Exception ex)
                {
                    ExceptionPolicy.HandleException(ex);
                }
            }
            return false;
        }

        public string GetConfigContent(string configName)
        {
            if (_parent != null)
            {
                try
                {
                    XmlNodeList childNodes = _parent.ChildNodes;
                    foreach (XmlNode item in childNodes)
                    {
                        if (item is XmlElement)
                        {
                            XmlElement xmlElement = (XmlElement)item;
                            if (xmlElement.GetAttribute("key") == configName)
                            {
                                return xmlElement.GetAttribute("value");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionPolicy.HandleException(ex);
                }
            }
            return "";
        }
    }
}
