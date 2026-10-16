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
    public class SysParameterBLL : BLLBase<string, SysParameter>
    {
        public SysParameterBLL(string repUri)
            : base(repUri)
        {
        }

        public CommandResult Save(SysParameter parameter)
        {
            SysParameter queryObject = GetByID(parameter.ID).QueryObject;
            if (queryObject != null)
            {
                return ProviderFactory.Create<IProvider<SysParameter, string>>(base.RepoUri).Update(parameter, queryObject);
            }
            return ProviderFactory.Create<IProvider<SysParameter, string>>(base.RepoUri).Insert(parameter);
        }

        public override CommandResult Add(SysParameter info)
        {
            return Save(info);
        }
    }
}
