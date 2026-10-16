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
    internal class StandardProvider : ProviderBase<Standard, Guid>, IBatchProvider<Standard, Guid>, IProvider<Standard, Guid>
    {
        public StandardProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Standard GetingItemByID(Guid id, DataContext dc)
        {
            DataLoadOptions dataLoadOptions = new DataLoadOptions();
            dataLoadOptions.LoadWith((Standard item) => item.Items);
            dc.LoadOptions = dataLoadOptions;
            Standard s = dc.GetTable<Standard>().SingleOrDefault((Standard item) => item.ID == id);
            if (s != null)
            {
                s.PhysicalItem = dc.GetTable<PhysicalItem>().SingleOrDefault((PhysicalItem item) => item.ID == s.PhysicalItemID);
            }
            return s;
        }

        protected override List<Standard> GetingItems(DataContext dc, SearchCondition search)
        {
            dc.Log = Console.Out;
            IQueryable<Standard> source = dc.GetTable<Standard>();
            var source2 = dc.GetTable<Standard>().Join(dc.GetTable<StandardItem>(), (Standard standard) => standard.ID, (StandardItem si) => si.StandardID, (Standard a, StandardItem si) => new
            {
                A = a,
                B = si
            });
            if (search is StandardSearchCondition)
            {
                StandardSearchCondition con = search as StandardSearchCondition;
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((Standard it) => it.ProjectID == con.ProjectID);
                    source2 = source2.Where(it => it.A.ProjectID == con.ProjectID);
                }
            }
            List<Standard> list = source.ToList();
            if (list != null && list.Count > 0)
            {
                List<StandardItem> list2 = source2.Select(it => it.B).ToList();
                List<PhysicalItem> list3 = dc.GetTable<PhysicalItem>().ToList();
                foreach (Standard s in list)
                {
                    if (list3 != null && list3.Count > 0)
                    {
                        s.PhysicalItem = list3.SingleOrDefault((PhysicalItem it) => it.ID == s.PhysicalItemID);
                    }
                    if (list2 != null && list2.Count > 0)
                    {
                        s.Items = list2.Where((StandardItem it) => it.StandardID == s.ID).ToList();
                    }
                }
            }
            return list;
        }

        protected override void UpdatingItem(Standard newVal, Standard original, DataContext dc)
        {
            dc.GetTable<Standard>().Attach(newVal, original);
            foreach (StandardItem item in newVal.Items)
            {
                StandardItem standardItem = ((original.Items != null) ? original.Items.SingleOrDefault((StandardItem it) => it.ID == item.ID) : null);
                if (standardItem != null)
                {
                    dc.GetTable<StandardItem>().Attach(item, standardItem);
                }
                else
                {
                    dc.GetTable<StandardItem>().InsertOnSubmit(item);
                }
            }
            if (original.Items == null || original.Items.Count <= 0)
            {
                return;
            }
            foreach (StandardItem item2 in original.Items)
            {
                if (newVal.Items.SingleOrDefault((StandardItem it) => it.ID == item2.ID) == null)
                {
                    dc.GetTable<StandardItem>().Attach(item2);
                    dc.GetTable<StandardItem>().DeleteOnSubmit(item2);
                }
            }
        }

        protected override void DeletingItem(Standard info, DataContext dc)
        {
            base.DeletingItem(info, dc);
            if (info.Items == null || info.Items.Count <= 0)
            {
                return;
            }
            foreach (StandardItem item in info.Items)
            {
                dc.GetTable<StandardItem>().Attach(item);
                dc.GetTable<StandardItem>().DeleteOnSubmit(item);
            }
        }

        public CommandResult BatchAdd(List<Standard> items, string projectID, ImportOption option)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                List<Standard> source = GetingItems(CreateDataContext(), new StandardSearchCondition
                {
                    ProjectID = projectID
                });
                foreach (Standard info in items)
                {
                    Standard original = source.SingleOrDefault((Standard it) => it.ID == info.ID);
                    if (original == null)
                    {
                        original = source.SingleOrDefault((Standard item) => item.ProjectID == info.ProjectID && item.Sex == info.Sex && item.Grade == info.Grade && item.PhysicalItemID == info.PhysicalItemID && item.IsForJiafen == info.IsForJiafen);
                    }
                    if (original != null)
                    {
                        if (info.Items != null && info.Items.Count > 0)
                        {
                            info.ID = original.ID;
                            info.Items.ForEach(delegate(StandardItem item)
                            {
                                item.StandardID = original.ID;
                            });
                            UpdatingItem(info, original, dataContext);
                        }
                        else
                        {
                            DeletingItem(info, dataContext);
                        }
                    }
                    else
                    {
                        InsertingItem(info, dataContext);
                    }
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

        public CommandResult BatchDelete(List<Standard> items, string projectID)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                foreach (Standard item in items)
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
