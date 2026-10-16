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
    public class StudentCheckRecordProvider : ProviderBase<StudentCheckRecord, Guid>
    {
        public StudentCheckRecordProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override StudentCheckRecord GetingItemByID(Guid id, DataContext dc)
        {
            return dc.GetTable<StudentCheckRecord>().SingleOrDefault((StudentCheckRecord item) => item.ID == id);
        }

        protected override List<StudentCheckRecord> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<StudentCheckRecord> source = dc.GetTable<StudentCheckRecord>();
            if (search is StudentCheckRecordSearchCondition)
            {
                StudentCheckRecordSearchCondition con = search as StudentCheckRecordSearchCondition;
                if (!string.IsNullOrEmpty(con.StudentID))
                {
                    source = source.Where((StudentCheckRecord item) => item.StudentID == con.StudentID);
                }
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((StudentCheckRecord item) => item.ProjectID == con.ProjectID);
                }
                if (con.CheckTime != null)
                {
                    source = source.Where((StudentCheckRecord item) => item.CheckTime >= con.CheckTime.Begin && item.CheckTime <= con.CheckTime.End);
                }
                if (!string.IsNullOrEmpty(con.Operator))
                {
                    source = source.Where((StudentCheckRecord item) => item.Operator == con.Operator);
                }
            }
            return source.ToList();
        }
    }
}
