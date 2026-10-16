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

namespace HH.TiYu.DAL.LinqProvider
{
    public class RoleProvider : ProviderBase<Role, string>
    {
        public RoleProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Role GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<Role>().SingleOrDefault((Role r) => r.ID == id);
        }

        protected override void DeletingItem(Role info, DataContext dc)
        {
            if (dc.GetTable<Operator>().Count((Operator it) => it.RoleID == info.ID) > 0)
            {
                throw new Exception("已经有操作员归属到此角色，不能删除。");
            }
            base.DeletingItem(info, dc);
        }
    }
}
