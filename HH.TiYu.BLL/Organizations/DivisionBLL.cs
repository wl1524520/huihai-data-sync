// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.BLL
{
    public class DivisionBLL : BLLBase<string, Division>
    {
        public DivisionBLL(string repoUri)
            : base(repoUri)
        {
        }

        public override QueryResultList<Division> GetItems(SearchCondition condition)
        {
            if (condition == null)
            {
                condition = new DivisionSearchCondition
                {
                    ProjectID = PhysicalProject.Current.ID
                };
            }
            else if (condition is DivisionSearchCondition)
            {
                (condition as DivisionSearchCondition).ProjectID = PhysicalProject.Current.ID;
            }
            return base.GetItems(condition);
        }

        public override CommandResult Add(Division info)
        {
            if (string.IsNullOrEmpty(info.ID))
            {
                info.ID = Guid.NewGuid().ToString();
            }
            DivisionSearchCondition condition = new DivisionSearchCondition
            {
                ProjectID = PhysicalProject.Current.ID
            };
            List<Division> queryObjects = GetItems(condition).QueryObjects;
            if (queryObjects != null && queryObjects.Exists((Division it) => it.Name == info.Name))
            {
                return new CommandResult(ResultCode.Fail, "已经存在同名区域，请重新设置名称");
            }
            if (string.IsNullOrEmpty(info.ProjectID) && PhysicalProject.Current != null)
            {
                info.ProjectID = PhysicalProject.Current.ID;
            }
            return base.Add(info);
        }

        public override CommandResult Update(Division info)
        {
            DivisionSearchCondition condition = new DivisionSearchCondition
            {
                ProjectID = PhysicalProject.Current.ID
            };
            List<Division> queryObjects = GetItems(condition).QueryObjects;
            if (queryObjects != null && queryObjects.Exists((Division it) => it.Name == info.Name && it.ID != info.ID))
            {
                return new CommandResult(ResultCode.Fail, "已经存在同名区域，请重新设置名称");
            }
            return base.Update(info);
        }
    }
}
