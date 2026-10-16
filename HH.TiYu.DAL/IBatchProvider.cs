// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;
using LJH.GeneralLibrary.ExceptionHandling;

namespace HH.TiYu.DAL
{
    public interface IBatchProvider<TInfo, TID> : IProvider<TInfo, TID> where TInfo : class, IEntity<TID>
    {
        CommandResult BatchAdd(List<TInfo> items, string projectID, ImportOption option);

        CommandResult BatchDelete(List<TInfo> items, string projectID);

        CommandResult BatchPatch(List<UpdateItem<TID>> patchs);
    }
}
