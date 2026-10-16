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
    public class AlarmProvider : ProviderBase<Alarm, Guid>, IBatchProvider<Alarm, Guid>, IProvider<Alarm, Guid>
    {
        public AlarmProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Alarm GetingItemByID(Guid id, DataContext dc)
        {
            return dc.GetTable<Alarm>().SingleOrDefault((Alarm item) => item.ID == id);
        }

        protected override List<Alarm> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<Alarm> source = dc.GetTable<Alarm>();
            if (search is AlarmSearchCondition)
            {
                AlarmSearchCondition con = search as AlarmSearchCondition;
                if (con.AlarmDateTime != null)
                {
                    source = source.Where((Alarm c) => c.AlarmDateTime >= con.AlarmDateTime.Begin);
                    source = source.Where((Alarm c) => c.AlarmDateTime <= con.AlarmDateTime.End);
                }
                if (con.AlarmType.HasValue)
                {
                    source = source.Where((Alarm c) => (int?)c.AlarmType == (int?)con.AlarmType);
                }
                if (con.OperatorID != null)
                {
                    source = source.Where((Alarm c) => c.OperatorID == con.OperatorID);
                }
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((Alarm c) => c.ProjectID == con.ProjectID);
                }
            }
            source = source.OrderBy((Alarm item) => item.AlarmDateTime);
            return source.ToList();
        }

        public CommandResult BatchAdd(List<Alarm> items, string projectID, ImportOption option)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                foreach (Alarm item in items)
                {
                    dataContext.GetTable<Alarm>().InsertOnSubmit(item);
                }
                dataContext.SubmitChanges();
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }

        public CommandResult BatchDelete(List<Alarm> items, string projectID)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                foreach (Alarm item in items)
                {
                    DeletingItem(item, dataContext);
                }
                dataContext.SubmitChanges();
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
            throw new NotImplementedException();
        }
    }
}
