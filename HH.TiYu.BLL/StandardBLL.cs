// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;

namespace HH.TiYu.BLL
{
    public class StandardBLL : BLLBase<Guid, Standard>
    {
        public StandardBLL(string repUri)
            : base(repUri)
        {
        }

        public override CommandResult Add(Standard info)
        {
            return Save(info);
        }

        public override CommandResult Update(Standard info)
        {
            return Save(info);
        }

        public CommandResult Save(Standard s)
        {
            try
            {
                List<Standard> queryObjects = GetItems(new StandardSearchCondition
                {
                    ProjectID = s.ProjectID
                }).QueryObjects;
                Standard original = queryObjects.SingleOrDefault((Standard item) => item.ID == s.ID);
                if (original == null)
                {
                    original = queryObjects.SingleOrDefault((Standard item) => item.ProjectID == s.ProjectID && item.Sex == s.Sex && item.Grade == s.Grade && item.PhysicalItemID == s.PhysicalItemID && item.IsForJiafen == s.IsForJiafen);
                }
                if (original != null)
                {
                    if (s.Items != null && s.Items.Count > 0)
                    {
                        s.ID = original.ID;
                        s.Items.ForEach(delegate(StandardItem item)
                        {
                            item.StandardID = original.ID;
                        });
                        return ProviderFactory.Create<IProvider<Standard, Guid>>(base.RepoUri).Update(s, original);
                    }
                    return ProviderFactory.Create<IProvider<Standard, Guid>>(base.RepoUri).Delete(original);
                }
                return ProviderFactory.Create<IProvider<Standard, Guid>>(base.RepoUri).Insert(s);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }
    }
}
