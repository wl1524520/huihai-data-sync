// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Linq;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.GeneralLibrary.Core.DAL;
using HH.GeneralLibrary.ExceptionHandling;

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

    }
}
