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
    public class PhysicalProjectProvider : ProviderBase<PhysicalProject, string>
    {
        public PhysicalProjectProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override PhysicalProject GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<PhysicalProject>().SingleOrDefault((PhysicalProject item) => item.ID == id);
        }

        protected override List<PhysicalProject> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<PhysicalProject> source = dc.GetTable<PhysicalProject>();
            if (search is PhysicalProjectSearchCondition)
            {
                PhysicalProjectSearchCondition con = search as PhysicalProjectSearchCondition;
                if (con.Class.HasValue)
                {
                    source = source.Where((PhysicalProject it) => (int)it.Class == (int)con.Class.Value);
                }
                if (!string.IsNullOrEmpty(con.Name))
                {
                    source = source.Where((PhysicalProject it) => it.Name == con.Name);
                }
                if (con.IDs != null && con.IDs.Count > 0)
                {
                    source = source.Where((PhysicalProject it) => con.IDs.Contains(it.ID));
                }
                if (!string.IsNullOrEmpty(con.Exclude))
                {
                    source = source.Where((PhysicalProject it) => it.ID != con.Exclude);
                }
            }
            return source.ToList();
        }

        protected override void InsertingItem(PhysicalProject info, DataContext dc)
        {
            if (string.IsNullOrEmpty(info.ID)) info.ID = Guid.NewGuid().ToString();
            if (!string.IsNullOrEmpty(info.TemplateProjectID))
            {
                var source = dc.GetTable<ProjectPhysicalItem>().Where(i => i.ProjectID == info.TemplateProjectID).ToList();
                foreach (var item in source)
                {
                    var copy = item.Clone(); copy.ID = Guid.NewGuid(); copy.ProjectID = info.ID;
                    dc.GetTable<ProjectPhysicalItem>().InsertOnSubmit(copy);
                }
            }
            base.InsertingItem(info, dc);
        }

        public override CommandResult Delete(PhysicalProject info)
        {
            try
            {
                using (var dc = CreateDataContext())
                {
                    dc.Connection.Open();
                    using (var transaction = dc.Connection.BeginTransaction())
                    {
                        dc.Transaction = transaction;
                        DeletingItem(info, dc);
                        dc.SubmitChanges();
                        transaction.Commit();
                    }
                }
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex) { return new CommandResult(ResultCode.Fail, ex.Message); }
        }

        protected override void DeletingItem(PhysicalProject info, DataContext dc)
        {
            if (dc.Transaction == null)
                throw new NotSupportedException("项目删除需要显式事务，请调用项目 Provider 的 Delete 方法");
            // Explicit project deletion removes its raw scores in the same transaction.
            foreach (var table in new[] { "StudentRawScore", "Student", "ProjectPhysicalItems", "Facility", "Division" })
                dc.ExecuteCommand("DELETE FROM [" + table + "] WHERE ProjectID = {0}", info.ID);
            base.DeletingItem(info, dc);
        }
    }
}
