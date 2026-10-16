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
    public class OperatorBLL : BLLBase<string, Operator>
    {
        public OperatorBLL(string repUri)
            : base(repUri)
        {
        }

        public bool Authentication(string logName, string pwd)
        {
            Operator queryObject = GetByID(logName).QueryObject;
            if (queryObject != null)
            {
                if (queryObject.ID == logName && queryObject.Password == pwd)
                {
                    Operator.Current = queryObject;
                    return true;
                }
                return false;
            }
            return false;
        }

        public override CommandResult Delete(Operator info)
        {
            if (!info.CanDelete)
            {
                return new CommandResult(ResultCode.Fail, "系统默认操作员,不能删除");
            }
            return base.Delete(info);
        }
    }
}
