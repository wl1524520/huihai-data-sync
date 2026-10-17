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
using LJH.GeneralLibrary.ExceptionHandling;

namespace HH.TiYu.DAL.LinqProvider
{
    internal class ProjectPhysicalItemProvider : ProviderBase<ProjectPhysicalItem, Guid>, IBatchProvider<ProjectPhysicalItem, Guid>, IProvider<ProjectPhysicalItem, Guid>
    {
        public ProjectPhysicalItemProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override ProjectPhysicalItem GetingItemByID(Guid id, DataContext dc)
        {
            return dc.GetTable<ProjectPhysicalItem>().SingleOrDefault((ProjectPhysicalItem item) => item.ID == id);
        }

        protected override List<ProjectPhysicalItem> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<ProjectPhysicalItem> source = dc.GetTable<ProjectPhysicalItem>();
            if (search is ProjectPhysicalItemSearchCondition)
            {
                ProjectPhysicalItemSearchCondition con = search as ProjectPhysicalItemSearchCondition;
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((ProjectPhysicalItem it) => it.ProjectID == con.ProjectID);
                }
                if (con.Sex.HasValue)
                {
                    source = source.Where((ProjectPhysicalItem it) => (int)it.Sex == (int)con.Sex.Value);
                }
            }
            return source.ToList();
        }

        public CommandResult BatchAdd(List<ProjectPhysicalItem> items, string projectID, ImportOption option)
        {
            try
            {
                using (DataContext dataContext = CreateDataContext())
                {
                    List<ProjectPhysicalItem> source = (from it in dataContext.GetTable<ProjectPhysicalItem>()
                        where it.ProjectID == projectID
                        select it).ToList();
                    foreach (ProjectPhysicalItem item in items)
                    {
                        item.ProjectID = projectID;
                        dataContext.GetTable<ProjectPhysicalItem>().InsertOnSubmit(item);
                        dataContext.GetTable<ProjectPhysicalItem>().DeleteAllOnSubmit(source.Where((ProjectPhysicalItem it) => it.Sex == item.Sex && it.Grade == item.Grade));
                    }
                    dataContext.SubmitChanges();
                }
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }

        public CommandResult BatchDelete(List<ProjectPhysicalItem> items, string projectID)
        {
            try
            {
                using (DataContext dataContext = CreateDataContext())
                {
                    foreach (ProjectPhysicalItem item in items)
                    {
                        DeletingItem(item, dataContext);
                    }
                    dataContext.SubmitChanges();
                }
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }

        public CommandResult BatchPatch(List<UpdateItem<Guid>> patchs)
        {
            throw new NotSupportedException("采集器不支持项目科目的批量修改");
        }
    }
}
