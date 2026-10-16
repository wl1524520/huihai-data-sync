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
    public class RoleBLL : BLLBase<string, Role>
    {
        public RoleBLL(string repUri)
            : base(repUri)
        {
        }

        public override CommandResult Delete(Role info)
        {
            if (!info.CanDelete)
            {
                return new CommandResult(ResultCode.Fail, "系统默认角色不能删除");
            }
            return base.Delete(info);
        }
    }
}
