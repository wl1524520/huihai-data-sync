// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;

namespace HH.TiYu.DAL.LinqProvider
{
    public class PhysicalItemProvider : ProviderBase<PhysicalItem, int>
    {
        public PhysicalItemProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override PhysicalItem GetingItemByID(int id, DataContext dc)
        {
            return dc.GetTable<PhysicalItem>().SingleOrDefault((PhysicalItem item) => item.ID == id);
        }

        protected override List<PhysicalItem> GetingItems(DataContext dc, SearchCondition search)
        {
            List<PhysicalItem> list = base.GetingItems(dc, search);
            if (list != null && list.Count > 0)
            {
                list = list.OrderBy((PhysicalItem item) => item.ID).ToList();
            }
            return list;
        }
    }
}
