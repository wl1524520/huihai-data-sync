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
    public class SportSuggestionProvider : ProviderBase<SportSuggestion, Guid>
    {
        public SportSuggestionProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override SportSuggestion GetingItemByID(Guid id, DataContext dc)
        {
            return dc.GetTable<SportSuggestion>().SingleOrDefault((SportSuggestion item) => item.ID == id);
        }

        protected override List<SportSuggestion> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<SportSuggestion> source = dc.GetTable<SportSuggestion>();
            if (search is SportSuggestionSearchCondition)
            {
                SportSuggestionSearchCondition con = search as SportSuggestionSearchCondition;
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((SportSuggestion it) => it.ProjectID == con.ProjectID);
                }
            }
            return source.ToList();
        }
    }
}
