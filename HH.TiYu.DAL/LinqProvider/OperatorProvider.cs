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
    public class OperatorProvider : ProviderBase<Operator, string>
    {
        public OperatorProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Operator GetingItemByID(string id, DataContext dc)
        {
            Operator item = dc.GetTable<Operator>().SingleOrDefault((Operator o) => o.ID == id);
            if (item != null)
            {
                item.Role = dc.GetTable<Role>().SingleOrDefault((Role r) => r.ID == item.RoleID);
            }
            return item;
        }

        protected override List<Operator> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<Operator> table = dc.GetTable<Operator>();
            List<Operator> list = table.ToList();
            if (list != null && list.Count > 0)
            {
                List<Role> list2 = dc.GetTable<Role>().ToList();
                if (list2 != null && list2.Count > 0)
                {
                    foreach (Operator item in list)
                    {
                        item.Role = list2.SingleOrDefault((Role r) => r.ID == item.RoleID);
                    }
                }
            }
            return list;
        }
    }
}
