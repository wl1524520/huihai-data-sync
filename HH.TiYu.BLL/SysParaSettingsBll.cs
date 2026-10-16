// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;

namespace HH.TiYu.BLL
{
    public class SysParaSettingsBll
    {
        public static CommandResult SaveSetting<T>(T info, string repUri, string projectID = null) where T : class
        {
            try
            {
                SysParameterBLL sysParameterBLL = new SysParameterBLL(repUri);
                Type typeFromHandle = typeof(T);
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeFromHandle);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    dataContractSerializer.WriteObject((Stream)memoryStream, (object)info);
                    memoryStream.Position = 0L;
                    byte[] array = new byte[memoryStream.Length];
                    memoryStream.Read(array, 0, (int)memoryStream.Length);
                    string paraValue = Encoding.UTF8.GetString(array);
                    string para = (string.IsNullOrEmpty(projectID) ? typeFromHandle.Name : (typeFromHandle.Name + "_" + projectID));
                    SysParameter parameter = new SysParameter(para, paraValue, string.Empty);
                    return sysParameterBLL.Save(parameter);
                }
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }

        public static T GetSetting<T>(string repUri, string projectID = null) where T : class
        {
            try
            {
                SysParameterBLL sysParameterBLL = new SysParameterBLL(repUri);
                SysParameter sysParameter = null;
                QueryResult<SysParameter> queryResult = null;
                Type typeFromHandle = typeof(T);
                string id = (string.IsNullOrEmpty(projectID) ? typeFromHandle.Name : (typeFromHandle.Name + "_" + projectID));
                queryResult = sysParameterBLL.GetByID(id);
                if (queryResult.QueryObject != null)
                {
                    sysParameter = queryResult.QueryObject;
                    string value = sysParameter.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(value);
                        using (MemoryStream memoryStream = new MemoryStream(bytes))
                        {
                            memoryStream.Position = 0L;
                            DataContractSerializer dataContractSerializer = new DataContractSerializer(typeFromHandle);
                            return dataContractSerializer.ReadObject((Stream)memoryStream) as T;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
            }
            return null;
        }

        public static T GetOrCreateSetting<T>(string repUri, string projectID = null) where T : class, new()
        {
            T val = GetSetting<T>(repUri, projectID);
            if (val == null)
            {
                val = new T();
            }
            return val;
        }
    }
}
