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
    public class DiscussionBLL : BLLBase<string, Discussion>
    {
        public DiscussionBLL(string repoUri)
            : base(repoUri)
        {
        }

        public override CommandResult Add(Discussion info)
        {
            CommandResult commandResult = base.Add(info);
            if (commandResult.Result == ResultCode.Successful)
            {
                QueryResultList<PhysicalScore> items = new PhysicalScoreBLL(base.RepoUri).GetItems(new PhysicalScoreSearchCondition
                {
                    ProjectID = info.ProjectID,
                    StudentID = info.StudentID
                });
                if (items.Result == ResultCode.Successful)
                {
                    List<PhysicalScore> queryObjects = items.QueryObjects;
                    if (queryObjects != null && queryObjects.Count > 0)
                    {
                        queryObjects.ForEach(delegate(PhysicalScore it)
                        {
                            PhysicalProject.Current.AddScore(it);
                        });
                    }
                }
            }
            return commandResult;
        }
    }
}
