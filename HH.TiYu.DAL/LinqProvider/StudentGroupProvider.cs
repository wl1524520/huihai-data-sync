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
    public class StudentGroupProvider : ProviderBase<StudentGroup, Guid>, IBatchProvider<StudentGroup, Guid>, IProvider<StudentGroup, Guid>
    {
        public StudentGroupProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override StudentGroup GetingItemByID(Guid id, DataContext dc)
        {
            DataLoadOptions dataLoadOptions = new DataLoadOptions();
            dataLoadOptions.LoadWith((StudentGroup item) => item.Items);
            dc.LoadOptions = dataLoadOptions;
            return dc.GetTable<StudentGroup>().SingleOrDefault((StudentGroup item) => item.ID == id);
        }

        protected override List<StudentGroup> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<StudentGroup> source = dc.GetTable<StudentGroup>();
            if (search is StudentGroupSearchCondition)
            {
                StudentGroupSearchCondition con = search as StudentGroupSearchCondition;
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((StudentGroup it) => it.ProjectID == con.ProjectID);
                }
            }
            List<StudentGroup> list = source.ToList();
            if (list != null && list.Count > 0)
            {
                List<StudentGroupPair> list2 = dc.GetTable<StudentGroupPair>().ToList();
                Dictionary<Guid, List<StudentGroupPair>> dictionary = new Dictionary<Guid, List<StudentGroupPair>>();
                foreach (StudentGroupPair item in list2)
                {
                    if (!dictionary.ContainsKey(item.GroupID))
                    {
                        dictionary[item.GroupID] = new List<StudentGroupPair>();
                    }
                    dictionary[item.GroupID].Add(item);
                }
                foreach (StudentGroup item2 in list)
                {
                    if (dictionary.ContainsKey(item2.ID))
                    {
                        item2.Items = dictionary[item2.ID];
                    }
                }
            }
            return list;
        }

        protected override void UpdatingItem(StudentGroup newVal, StudentGroup original, DataContext dc)
        {
            foreach (StudentGroupPair item in newVal.Items)
            {
                StudentGroupPair studentGroupPair = ((original.Items != null) ? original.Items.SingleOrDefault((StudentGroupPair it) => it.ID == item.ID) : null);
                if (studentGroupPair != null)
                {
                    dc.GetTable<StudentGroupPair>().Attach(item, studentGroupPair);
                }
                else
                {
                    dc.GetTable<StudentGroupPair>().InsertOnSubmit(item);
                }
            }
            if (original.Items != null && original.Items.Count > 0)
            {
                foreach (StudentGroupPair item2 in original.Items)
                {
                    if (newVal.Items.SingleOrDefault((StudentGroupPair it) => it.ID == item2.ID) == null)
                    {
                        dc.GetTable<StudentGroupPair>().Attach(item2);
                        dc.GetTable<StudentGroupPair>().DeleteOnSubmit(item2);
                    }
                }
            }
            base.UpdatingItem(newVal, original, dc);
        }

        public CommandResult BatchAdd(List<StudentGroup> gps, string projectID, ImportOption option)
        {
            try
            {
                List<StudentGroup> source = GetingItems(CreateDataContext(), new StudentGroupSearchCondition
                {
                    ProjectID = projectID
                });
                DataContext dataContext = CreateDataContext();
                foreach (StudentGroup gp in gps)
                {
                    gp.ProjectID = projectID;
                    StudentGroup studentGroup = source.FirstOrDefault((StudentGroup it) => it.Sex == gp.Sex && it.Number == gp.Number && it.PhysicalItems == gp.PhysicalItems && it.FacilityID == gp.FacilityID && it.PlanDate == gp.PlanDate);
                    if (studentGroup != null && option == ImportOption.Override)
                    {
                        gp.ID = studentGroup.ID;
                        gp.Items.ForEach(delegate(StudentGroupPair it)
                        {
                            it.GroupID = gp.ID;
                        });
                        UpdatingItem(gp, studentGroup, dataContext);
                    }
                    else
                    {
                        InsertingItem(gp, dataContext);
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

        public CommandResult BatchDelete(List<StudentGroup> groups, string projectID)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                foreach (StudentGroup group in groups)
                {
                    dataContext.GetTable<StudentGroup>().Attach(group);
                    dataContext.GetTable<StudentGroup>().DeleteOnSubmit(group);
                    if (group.Items != null && group.Items.Count > 0)
                    {
                        dataContext.GetTable<StudentGroupPair>().AttachAll(group.Items);
                        dataContext.GetTable<StudentGroupPair>().DeleteAllOnSubmit(group.Items);
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

        public CommandResult BatchPatch(List<UpdateItem<Guid>> patchs)
        {
            try
            {
                List<Guid> ids = patchs.Select((UpdateItem<Guid> it) => it.ID).Distinct().ToList();
                DataContext dataContext = CreateDataContext();
                List<StudentGroup> list = (from it in dataContext.GetTable<StudentGroup>()
                    where ids.Contains(it.ID)
                    select it).ToList();
                Dictionary<Guid, StudentGroup> dictionary = new Dictionary<Guid, StudentGroup>();
                foreach (StudentGroup item in list)
                {
                    if (!dictionary.ContainsKey(item.ID))
                    {
                        dictionary.Add(item.ID, item);
                    }
                }
                foreach (UpdateItem<Guid> patch in patchs)
                {
                    if (dictionary.ContainsKey(patch.ID))
                    {
                        StudentGroup studentGroup = dictionary[patch.ID];
                        string key = patch.Key;
                        if (key == "PlanDate")
                        {
                            studentGroup.PlanDate = patch.Value;
                        }
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
    }
}
