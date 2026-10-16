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
    public class AlarmInfoBLL : BLLBase<Guid, Alarm>
    {
        public AlarmInfoBLL(string repoUri)
            : base(repoUri)
        {
        }

        public CommandResult AddAlarms(List<Alarm> alarms, PhysicalProject project)
        {
            return ProviderFactory.Create<IBatchProvider<Alarm, Guid>>(base.RepoUri).BatchAdd(alarms, project.ID, ImportOption.Ignore);
        }
    }
}
