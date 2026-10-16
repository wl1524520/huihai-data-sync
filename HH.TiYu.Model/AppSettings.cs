// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using HH.TiYu.Model.Security;
using Jurassic;
using LJH.GeneralLibrary;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.SoftDog;
using Newtonsoft.Json;

namespace HH.TiYu.Model
{
    public class AppSettings
    {
        private static AppSettings _instance = null;

        private XmlDocument _doc = null;

        private XmlNode _parent = null;

        private string _path;

        private string _ConnectString;

        private string _RemoteDB;

        private byte _HostCommport;

        private byte _CardReaderCommport;

        private byte _BarcodeReaderCommport;

        private bool _DatabaseNeedUpgrade;

        private bool _RememberLogID;

        private string _ScorebillModal;

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
                if (string.IsNullOrEmpty(RemoteDB))
                {
                    return LocalDB;
                }
                return RemoteDB;
            }
        }

        public string RemoteDB
        {
            get
            {
                if (!string.IsNullOrEmpty(_RemoteDB))
                {
                    return _RemoteDB;
                }
                return null;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _RemoteDB = value;
                    SaveConfig("RemoteDB", _RemoteDB);
                }
                else
                {
                    _RemoteDB = value;
                    SaveConfig("RemoteDB", string.Empty);
                }
            }
        }

        public string RemoteServer
        {
            get
            {
                return GetConfigContent("RemoteServer");
            }
            set
            {
                SaveConfig("RemoteServer", value.ToString());
            }
        }

        public string RemotePort
        {
            get
            {
                return GetConfigContent("RemotePort");
            }
            set
            {
                SaveConfig("RemotePort", value.ToString());
            }
        }

        public string LocalDB { get; set; }

        public string StudentRawScoreDB
        {
            get
            {
                // Collector single-database policy; replaces the former Model.dll build patch.
                return "SQLITE:Data Source=" + Path.Combine(Application.StartupPath, "HuihaiTiyu.db");
            }
        }

        public bool IsOffLine { get; set; }

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

        public byte HostCommport
        {
            get
            {
                return _HostCommport;
            }
            set
            {
                if (_HostCommport != value)
                {
                    _HostCommport = value;
                    SaveConfig("HostCommport", value.ToString());
                }
            }
        }

        public byte CardReaderCommport
        {
            get
            {
                return _CardReaderCommport;
            }
            set
            {
                if (_CardReaderCommport != value)
                {
                    _CardReaderCommport = value;
                    SaveConfig("CardReaderCommport", value.ToString());
                }
            }
        }

        public byte BarcodeReaderCommport
        {
            get
            {
                return _BarcodeReaderCommport;
            }
            set
            {
                if (_BarcodeReaderCommport != value)
                {
                    _BarcodeReaderCommport = value;
                    SaveConfig("BarcodeReaderCommport", value.ToString());
                }
            }
        }

        public bool DatabaseNeedUpgrade
        {
            get
            {
                return _DatabaseNeedUpgrade;
            }
            set
            {
                if (_DatabaseNeedUpgrade != value)
                {
                    _DatabaseNeedUpgrade = value;
                    SaveConfig("DatabaseNeedUpgrade", value.ToString());
                }
            }
        }

        public bool RememberLogID
        {
            get
            {
                return _RememberLogID;
            }
            set
            {
                if (_RememberLogID != value)
                {
                    _RememberLogID = value;
                    SaveConfig("RememberLogID", _RememberLogID.ToString());
                }
            }
        }

        public string ScorebillModal
        {
            get
            {
                return _ScorebillModal;
            }
            set
            {
                _ScorebillModal = value;
                SaveConfig("ScorebillModal", value);
            }
        }

        public bool EnableTTS
        {
            get
            {
                bool result = true;
                string configContent = GetConfigContent("EnableTTS");
                if (!string.IsNullOrEmpty(configContent))
                {
                    bool.TryParse(configContent, out result);
                }
                return result;
            }
            set
            {
                SaveConfig("EnableTTS", value.ToString());
            }
        }

        public bool VoiceForNexT
        {
            get
            {
                bool result = true;
                string configContent = GetConfigContent("VoiceForNexT");
                if (!string.IsNullOrEmpty(configContent))
                {
                    bool.TryParse(configContent, out result);
                }
                return result;
            }
            set
            {
                SaveConfig("VoiceForNexT", value.ToString());
            }
        }

        public bool UseLastCamera
        {
            get
            {
                bool result = false;
                string configContent = GetConfigContent("UseLastCamera");
                if (!string.IsNullOrEmpty(configContent))
                {
                    bool.TryParse(configContent, out result);
                }
                return result;
            }
            set
            {
                SaveConfig("UseLastCamera", value.ToString());
            }
        }

        public int? MinGrade { get; set; }

        public int? MaxGrade { get; set; }

        public bool 用于抽测 { get; set; }

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
                    _RemoteDB = GetConfigContent("RemoteDB");
                    string configContent = GetConfigContent("DatabaseNeedUpgrade");
                    bool.TryParse(configContent, out _DatabaseNeedUpgrade);
                    configContent = GetConfigContent("CardReaderCommport");
                    byte.TryParse(configContent, out _CardReaderCommport);
                    configContent = GetConfigContent("BarcodeReaderCommport");
                    byte.TryParse(configContent, out _BarcodeReaderCommport);
                    configContent = GetConfigContent("HostCommport");
                    byte.TryParse(configContent, out _HostCommport);
                    configContent = GetConfigContent("RememberLogID");
                    bool.TryParse(configContent, out _RememberLogID);
                    _ScorebillModal = GetConfigContent("ScorebillModal");
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
