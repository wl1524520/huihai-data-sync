using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using HH.TiYu.Model;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;

namespace HH.TiYu.DAL.LinqProvider
{
    public class SyncConfigProvider : ProviderBase<SyncConfigEntry, string>
    {
        public SyncConfigProvider(string connStr, MappingSource ms) : base(connStr, ms) { }

        protected override SyncConfigEntry GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<SyncConfigEntry>().SingleOrDefault(row => row.ID == id);
        }

        protected override List<SyncConfigEntry> GetingItems(DataContext dc, SearchCondition search)
        {
            if (search != null) throw new NotSupportedException("同步配置不支持筛选条件");
            return dc.GetTable<SyncConfigEntry>().ToList();
        }

        /// <summary>Save all supplied pairs in one transaction, preserving unrelated keys.</summary>
        public void SavePairs(IDictionary<string, string> values)
        {
            using (var dc = CreateDataContext())
            {
                dc.Connection.Open();
                using (var transaction = dc.Connection.BeginTransaction())
                {
                    dc.Transaction = transaction;
                    var rows = dc.GetTable<SyncConfigEntry>().ToDictionary(row => row.ID, StringComparer.Ordinal);
                    foreach (var pair in values)
                    {
                        if (string.IsNullOrEmpty(pair.Key)) throw new ArgumentException("配置键不能为空");
                        SyncConfigEntry row;
                        if (!rows.TryGetValue(pair.Key, out row))
                        {
                            row = new SyncConfigEntry { ID = pair.Key };
                            dc.GetTable<SyncConfigEntry>().InsertOnSubmit(row);
                        }
                        row.Value = pair.Value ?? string.Empty;
                    }
                    dc.SubmitChanges();
                    transaction.Commit();
                }
            }
        }
    }
}
