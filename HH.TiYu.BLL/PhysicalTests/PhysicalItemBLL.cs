// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Linq;
using HH.TiYu.Model;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.BLL
{
    public class PhysicalItemBLL : BLLBase<int, PhysicalItem>
    {
        public PhysicalItemBLL(string repoUri)
            : base(repoUri)
        {
        }

        public override CommandResult Add(PhysicalItem info)
        {
            if (info.ID <= 0 && info.ID < 100)
            {
                return new CommandResult(ResultCode.Fail, "该体能测试项目的ID号不正确,用户创建的测试项目ID号要大于99");
            }
            List<PhysicalItem> queryObjects = GetItems(null).QueryObjects;
            if (queryObjects != null && queryObjects.Exists((PhysicalItem it) => it.Name == info.Name))
            {
                return new CommandResult(ResultCode.Fail, string.Format("系统中已经存在名称为 {0} 的测试项目,请选择其它的名称", info.Name));
            }
            return base.Add(info);
        }

        public override CommandResult Update(PhysicalItem info)
        {
            List<PhysicalItem> queryObjects = GetItems(null).QueryObjects;
            if (queryObjects != null && queryObjects.Exists((PhysicalItem it) => it.Name == info.Name && it.ID != info.ID))
            {
                return new CommandResult(ResultCode.Fail, string.Format("系统中已经存在名称为 {0} 的测试项目,请选择其它的名称", info.Name));
            }
            return base.Update(info);
        }

        public override CommandResult Delete(PhysicalItem info)
        {
            if (info.HasMachine)
            {
                return new CommandResult(ResultCode.Fail, "该体能测试项目是系统默认的项目，不能删除");
            }
            return base.Delete(info);
        }

        public QueryResultList<PhysicalItem> GetZhongkaoItems()
        {
            QueryResultList<PhysicalItem> items = GetItems(null);
            List<PhysicalItem> queryObjects = items.QueryObjects;
            int[] array = new int[16]
            {
                10, 11, 18, 15, 6, 5, 12, 14, 13, 7,
                8, 9, 1, 2, 4, 3
            };
            if (queryObjects != null && queryObjects.Count > 0)
            {
                List<PhysicalItem> list = new List<PhysicalItem>();
                int[] array2 = array;
                foreach (int pID in array2)
                {
                    PhysicalItem physicalItem = queryObjects.SingleOrDefault((PhysicalItem it) => it.ID == pID);
                    if (physicalItem != null)
                    {
                        list.Add(physicalItem);
                        queryObjects.Remove(physicalItem);
                    }
                }
                list.AddRange(queryObjects);
                items.QueryObjects = list;
            }
            return items;
        }
    }
}
