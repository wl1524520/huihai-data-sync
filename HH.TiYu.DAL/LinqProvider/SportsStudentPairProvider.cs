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
    public class SportsStudentPairProvider : ProviderBase<SportsStudentPair, Guid>
    {
        public SportsStudentPairProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override SportsStudentPair GetingItemByID(Guid id, DataContext dc)
        {
            return dc.GetTable<SportsStudentPair>().SingleOrDefault((SportsStudentPair item) => item.ID == id);
        }

        protected override List<SportsStudentPair> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<SportsStudentPair> source = dc.GetTable<SportsStudentPair>();
            if (search is SportsStudentPairSearchCondition)
            {
                SportsStudentPairSearchCondition con = search as SportsStudentPairSearchCondition;
                if (!string.IsNullOrEmpty(con.StudentID))
                {
                    source = source.Where((SportsStudentPair it) => it.StudentID == con.StudentID);
                }
                if (con.SportsID.HasValue)
                {
                    source = source.Where((SportsStudentPair it) => it.SportsID == con.SportsID.Value);
                }
                if (con.Sports != null && con.Sports.Count > 0)
                {
                    source = source.Where((SportsStudentPair it) => con.Sports.Contains(it.SportsID));
                }
                if (con.Students != null && con.Students.Count > 0)
                {
                    source = source.Where((SportsStudentPair it) => con.Students.Contains(it.StudentID));
                }
                if (!string.IsNullOrEmpty(con.GroupID))
                {
                    source = source.Where((SportsStudentPair it) => it.GroupID == con.GroupID);
                }
                if (con.State.HasValue)
                {
                    source = source.Where((SportsStudentPair it) => it.State == con.State.Value);
                }
            }
            return source.ToList();
        }
    }
}
