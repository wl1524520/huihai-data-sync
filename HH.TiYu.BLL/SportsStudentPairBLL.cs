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
    public class SportsStudentPairBLL : BLLBase<Guid, SportsStudentPair>
    {
        public SportsStudentPairBLL(string repoUri)
            : base(repoUri)
        {
        }

        public CommandResult Import(List<SportsStudentPair> items)
        {
            if (items != null && items.Count > 0)
            {
                IProvider<SportsStudentPair, Guid> provider = ProviderFactory.Create<IProvider<SportsStudentPair, Guid>>(base.RepoUri);
                List<SportsStudentPair> queryObjects = provider.GetItems(null).QueryObjects;
                IUnitWork unitWork = provider.CreateUnitWork();
                foreach (SportsStudentPair pair in items)
                {
                    SportsStudentPair sportsStudentPair = queryObjects.FirstOrDefault((SportsStudentPair it) => it.SportsID == pair.SportsID && it.StudentID == pair.StudentID);
                    if (sportsStudentPair != null)
                    {
                        SportsStudentPair sportsStudentPair2 = sportsStudentPair.Clone();
                        CopyValue(pair, sportsStudentPair2);
                        provider.Update(sportsStudentPair2, sportsStudentPair, unitWork);
                    }
                    else
                    {
                        provider.Insert(pair, unitWork);
                    }
                }
                return unitWork.Commit();
            }
            return new CommandResult(ResultCode.Fail, "没有记录");
        }

        private void CopyValue(SportsStudentPair source, SportsStudentPair des)
        {
            des.GroupID = source.GroupID;
            des.Channel = source.Channel;
        }

        public CommandResult SaveScore(SportsStudentPair item, decimal score)
        {
            SportsStudentPair sportsStudentPair = item.Clone();
            sportsStudentPair.Score = score;
            sportsStudentPair.State = 1;
            Sports queryObject = ProviderFactory.Create<IProvider<Sports, Guid>>(base.RepoUri).GetByID(item.SportsID).QueryObject;
            if (queryObject == null || queryObject.PhysicalItem == null)
            {
                return new CommandResult(ResultCode.Fail, "没有找到对就的比赛项目");
            }
            IProvider<SportsStudentPair, Guid> provider = ProviderFactory.Create<IProvider<SportsStudentPair, Guid>>(base.RepoUri);
            IUnitWork unitWork = provider.CreateUnitWork();
            SportsStudentPairSearchCondition sportsStudentPairSearchCondition = new SportsStudentPairSearchCondition();
            sportsStudentPairSearchCondition.SportsID = item.SportsID;
            sportsStudentPairSearchCondition.GroupID = item.GroupID;
            List<SportsStudentPair> list = provider.GetItems(sportsStudentPairSearchCondition).QueryObjects;
            if (list == null || list.Count == 0)
            {
                return new CommandResult(ResultCode.Fail, "比赛项目没有参赛人员");
            }
            list.RemoveAll((SportsStudentPair it) => it.ID == item.ID || !it.Score.HasValue);
            if (queryObject.PhysicalItem.SortMode == 0 || list.Count == 0)
            {
                sportsStudentPair.Rank = 1;
                provider.Update(sportsStudentPair, item, unitWork);
            }
            else
            {
                list.Add(sportsStudentPair);
                if (queryObject.PhysicalItem.SortMode == 1)
                {
                    list = list.OrderBy((SportsStudentPair it) => it.Score).ToList();
                }
                else if (queryObject.PhysicalItem.SortMode == 2)
                {
                    list = list.OrderByDescending((SportsStudentPair it) => it.Score).ToList();
                }
                int num = 1;
                for (int num2 = 0; num2 < list.Count; num2++)
                {
                    if (num2 != 0 && !(list[num2].Score == list[num2 - 1].Score))
                    {
                        num++;
                    }
                    if (list[num2].ID == item.ID)
                    {
                        sportsStudentPair.Rank = num;
                        provider.Update(sportsStudentPair, item, unitWork);
                    }
                    else
                    {
                        SportsStudentPair sportsStudentPair2 = list[num2].Clone();
                        sportsStudentPair2.Rank = num;
                        provider.Update(sportsStudentPair2, list[num2], unitWork);
                    }
                }
            }
            CommandResult commandResult = unitWork.Commit();
            if (commandResult.Result == ResultCode.Successful)
            {
                item.Score = sportsStudentPair.Score;
                item.Rank = sportsStudentPair.Rank;
                item.State = sportsStudentPair.State;
            }
            return commandResult;
        }

        public CommandResult DeleteScore(SportsStudentPair item)
        {
            SportsStudentPair sportsStudentPair = item.Clone();
            sportsStudentPair.Score = null;
            sportsStudentPair.Rank = null;
            sportsStudentPair.State = 0;
            Sports queryObject = ProviderFactory.Create<IProvider<Sports, Guid>>(base.RepoUri).GetByID(item.SportsID).QueryObject;
            if (queryObject == null || queryObject.PhysicalItem == null)
            {
                return new CommandResult(ResultCode.Fail, "没有找到对就的比赛项目");
            }
            IProvider<SportsStudentPair, Guid> provider = ProviderFactory.Create<IProvider<SportsStudentPair, Guid>>(base.RepoUri);
            IUnitWork unitWork = provider.CreateUnitWork();
            SportsStudentPairSearchCondition sportsStudentPairSearchCondition = new SportsStudentPairSearchCondition();
            sportsStudentPairSearchCondition.SportsID = item.SportsID;
            List<SportsStudentPair> list = provider.GetItems(sportsStudentPairSearchCondition).QueryObjects;
            if (list == null || list.Count == 0)
            {
                return new CommandResult(ResultCode.Fail, "比赛项目没有参赛人员");
            }
            list.RemoveAll((SportsStudentPair it) => it.ID == item.ID || !it.Score.HasValue);
            provider.Update(sportsStudentPair, item, unitWork);
            if (queryObject.PhysicalItem.SortMode == 1)
            {
                list = list.OrderBy((SportsStudentPair it) => it.Score).ToList();
            }
            else if (queryObject.PhysicalItem.SortMode == 2)
            {
                list = list.OrderByDescending((SportsStudentPair it) => it.Score).ToList();
            }
            int num = 1;
            for (int num2 = 0; num2 < list.Count; num2++)
            {
                if (num2 != 0 && !(list[num2].Score == list[num2 - 1].Score))
                {
                    num++;
                }
                SportsStudentPair sportsStudentPair2 = list[num2].Clone();
                sportsStudentPair2.Rank = num;
                provider.Update(sportsStudentPair2, list[num2], unitWork);
            }
            CommandResult commandResult = unitWork.Commit();
            if (commandResult.Result == ResultCode.Successful)
            {
                item.Score = sportsStudentPair.Score;
                item.Rank = sportsStudentPair.Rank;
                item.State = sportsStudentPair.State;
            }
            return commandResult;
        }

        public CommandResult Delete(List<SportsStudentPair> items)
        {
            if (items != null && items.Count > 0)
            {
                IProvider<SportsStudentPair, Guid> provider = ProviderFactory.Create<IProvider<SportsStudentPair, Guid>>(base.RepoUri);
                IUnitWork unitWork = provider.CreateUnitWork();
                foreach (SportsStudentPair item in items)
                {
                    provider.Delete(item, unitWork);
                }
                return unitWork.Commit();
            }
            return new CommandResult(ResultCode.Fail, "没有记录");
        }
    }
}
