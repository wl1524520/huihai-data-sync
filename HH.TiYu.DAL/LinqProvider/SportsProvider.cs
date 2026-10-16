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
    public class SportsProvider : ProviderBase<Sports, Guid>
    {
        public SportsProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Sports GetingItemByID(Guid id, DataContext dc)
        {
            Sports s = dc.GetTable<Sports>().SingleOrDefault((Sports item) => item.ID == id);
            if (s != null)
            {
                s.PhysicalItem = dc.GetTable<PhysicalItem>().SingleOrDefault((PhysicalItem it) => it.ID == s.PhysicalItemID);
            }
            return s;
        }

        protected override List<Sports> GetingItems(DataContext dc, SearchCondition search)
        {
            List<Sports> list = dc.GetTable<Sports>().ToList();
            if (list != null && list.Count > 0)
            {
                List<PhysicalItem> source = dc.GetTable<PhysicalItem>().ToList();
                foreach (Sports sports in list)
                {
                    sports.PhysicalItem = source.SingleOrDefault((PhysicalItem it) => it.ID == sports.PhysicalItemID);
                }
            }
            return list;
        }

        protected override void DeletingItem(Sports info, DataContext dc)
        {
            List<SportsStudentPair> list = (from it in dc.GetTable<SportsStudentPair>()
                where it.SportsID == info.ID
                select it).ToList();
            if (list != null)
            {
                foreach (SportsStudentPair item in list)
                {
                    dc.GetTable<SportsStudentPair>().DeleteOnSubmit(item);
                }
            }
            base.DeletingItem(info, dc);
        }
    }
}
