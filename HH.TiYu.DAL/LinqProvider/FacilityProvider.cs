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
    internal class FacilityProvider : ProviderBase<Facility, string>
    {
        public FacilityProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Facility GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<Facility>().SingleOrDefault((Facility it) => it.ID == id);
        }

        protected override List<Facility> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<Facility> source = dc.GetTable<Facility>();
            if (search is FacilitySearchCondition)
            {
                FacilitySearchCondition con = search as FacilitySearchCondition;
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((Facility it) => it.ProjectID == con.ProjectID);
                }
                if (!string.IsNullOrEmpty(con.Name))
                {
                    source = source.Where((Facility it) => it.Name == con.Name);
                }
            }
            return source.ToList();
        }

        protected override void DeletingItem(Facility info, DataContext dc)
        {
            if (dc.GetTable<Student>().Count((Student it) => it.FacilityID == info.ID) > 0)
            {
                throw new Exception("已经有学生归到此学校，如果确实要删除此学校，请先更改相关学生的所属学校");
            }
            base.DeletingItem(info, dc);
        }
    }
}
