// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.GeneralLibrary.Core.DAL;
using HH.GeneralLibrary.Core.DAL.Linq;

namespace HH.TiYu.DAL.LinqProvider
{
    internal class DivisionProvider : ProviderBase<Division, string>
    {
        public DivisionProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Division GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<Division>().SingleOrDefault((Division item) => item.ID == id);
        }

        protected override List<Division> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<Division> source = dc.GetTable<Division>();
            if (search is DivisionSearchCondition)
            {
                DivisionSearchCondition con = search as DivisionSearchCondition;
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((Division it) => it.ProjectID == con.ProjectID);
                }
            }
            return source.ToList();
        }

        protected override void DeletingItem(Division info, DataContext dc)
        {
            if (dc.GetTable<Division>().Count((Division it) => it.Parent == info.ID) > 0)
            {
                throw new Exception("区域下已经有子区域，请先将所有子区域删除，再删除此区域");
            }
            if (dc.GetTable<Facility>().Count((Facility it) => it.DivisionID == info.ID) > 0)
            {
                throw new Exception("已经有学校归到此区域，如果确实要删除此区域，请先更改相关学校的所属区域");
            }
            base.DeletingItem(info, dc);
        }
    }
}
