// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Linq;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;

namespace HH.TiYu.BLL
{
    public class FacilityBLL : BLLBase<string, Facility>
    {
        public FacilityBLL(string repoUri)
            : base(repoUri)
        {
        }

        public override QueryResultList<Facility> GetItems(SearchCondition condition)
        {
            if (condition == null)
            {
                condition = new FacilitySearchCondition
                {
                    ProjectID = PhysicalProject.Current.ID
                };
            }
            else if (condition is FacilitySearchCondition)
            {
                (condition as FacilitySearchCondition).ProjectID = PhysicalProject.Current.ID;
            }
            return base.GetItems(condition);
        }

        public override CommandResult Add(Facility info)
        {
            if (string.IsNullOrEmpty(info.ID))
            {
                info.ID = Guid.NewGuid().ToString();
            }
            FacilitySearchCondition condition = new FacilitySearchCondition
            {
                ProjectID = PhysicalProject.Current.ID
            };
            List<Facility> queryObjects = GetItems(condition).QueryObjects;
            if (queryObjects != null && queryObjects.Exists((Facility it) => it.Name == info.Name))
            {
                return new CommandResult(ResultCode.Fail, "已经存在同名学校，请重新设置名称");
            }
            if (string.IsNullOrEmpty(info.ProjectID) && PhysicalProject.Current != null)
            {
                info.ProjectID = PhysicalProject.Current.ID;
            }
            return base.Add(info);
        }

        public override CommandResult Update(Facility info)
        {
            FacilitySearchCondition condition = new FacilitySearchCondition
            {
                ProjectID = PhysicalProject.Current.ID
            };
            List<Facility> queryObjects = GetItems(condition).QueryObjects;
            if (queryObjects != null && queryObjects.Exists((Facility it) => it.Name == info.Name && it.ID != info.ID))
            {
                return new CommandResult(ResultCode.Fail, "已经存在同名学校，请重新设置名称");
            }
            return base.Update(info);
        }

        public CommandResult Import(string name, string division, bool overWrite)
        {
            try
            {
                List<Facility> queryObjects = GetItems(new FacilitySearchCondition
                {
                    ProjectID = PhysicalProject.Current.ID
                }).QueryObjects;
                if (queryObjects != null && queryObjects.Exists((Facility it) => it.Name == name) && !overWrite)
                {
                    return new CommandResult(ResultCode.Fail, "已经存在同名学校，请重新设置名称");
                }
                Division division2 = null;
                if (!string.IsNullOrEmpty(division))
                {
                    List<Division> queryObjects2 = new DivisionBLL(base.RepoUri).GetItems(new DivisionSearchCondition
                    {
                        ProjectID = PhysicalProject.Current.ID
                    }).QueryObjects;
                    if (queryObjects2 != null)
                    {
                        division2 = queryObjects2.SingleOrDefault((Division it) => it.Name == division);
                    }
                    if (division2 == null)
                    {
                        division2 = new Division
                        {
                            Name = division
                        };
                        CommandResult commandResult = new DivisionBLL(base.RepoUri).Add(division2);
                        if (commandResult.Result != ResultCode.Successful)
                        {
                            division2 = null;
                        }
                    }
                }
                Facility facility = queryObjects.SingleOrDefault((Facility it) => it.Name == name);
                if (facility == null)
                {
                    Facility t = new Facility
                    {
                        ID = Guid.NewGuid().ToString(),
                        ProjectID = PhysicalProject.Current.ID,
                        Name = name,
                        DivisionID = ((division2 != null) ? division2.ID : null)
                    };
                    return ProviderFactory.Create<IProvider<Facility, string>>(base.RepoUri).Insert(t);
                }
                Facility facility2 = facility.Clone();
                facility2.DivisionID = ((division2 != null) ? division2.ID : null);
                return ProviderFactory.Create<IProvider<Facility, string>>(base.RepoUri).Update(facility2, facility);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }
    }
}
