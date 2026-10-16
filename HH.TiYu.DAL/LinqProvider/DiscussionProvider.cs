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
    public class DiscussionProvider : ProviderBase<Discussion, string>
    {
        public DiscussionProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Discussion GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<Discussion>().SingleOrDefault((Discussion item) => item.ID == id);
        }

        protected override List<Discussion> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<Discussion> source = dc.GetTable<Discussion>();
            if (search is DiscussionSearchCondition)
            {
                DiscussionSearchCondition con = search as DiscussionSearchCondition;
                if (con.CreateTime != null)
                {
                    source = source.Where((Discussion c) => c.CreateTime >= con.CreateTime.Begin);
                    source = source.Where((Discussion c) => c.CreateTime <= con.CreateTime.End);
                }
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((Discussion c) => c.ProjectID == con.ProjectID);
                }
                if (!string.IsNullOrEmpty(con.StudentID))
                {
                    source = source.Where((Discussion c) => c.StudentID == con.StudentID);
                }
                if (!string.IsNullOrEmpty(con.Name))
                {
                    source = source.Where((Discussion c) => c.Name == con.Name);
                }
                if (con.ScoreID.HasValue)
                {
                    source = source.Where((Discussion it) => it.ScoreID == con.ScoreID.Value);
                }
            }
            source = source.OrderBy((Discussion item) => item.ID);
            return source.ToList();
        }

        protected override void InsertingItem(Discussion info, DataContext dc)
        {
            PhysicalScore physicalScore = dc.GetTable<PhysicalScore>().SingleOrDefault((PhysicalScore it) => it.ID == info.ScoreID);
            if (physicalScore == null)
            {
                throw new Exception("没有找到复议相关的学生成绩");
            }
            if (physicalScore.GetProperty("复议") != null)
            {
                throw new Exception("此成绩已经复议，不能再次复议");
            }
            if (string.IsNullOrEmpty(info.ID))
            {
                info.ID = new AutoNumberCreater(base.SqlURI, _MappingResource).CreateNumber("FY", "yyyyMMdd", 3, "Discussion");
            }
            if (string.IsNullOrEmpty(info.ID))
            {
                throw new Exception("创建编号失败");
            }
            base.InsertingItem(info, dc);
            if (info.ResolveType != ResolveType.当前成绩有效)
            {
                if (info.ResolveType == ResolveType.取消当前成绩)
                {
                    physicalScore.SpecialType = SpecialScoreType.作废;
                    physicalScore.SetProperty("备注", "复议后作废");
                }
                else if (info.ResolveType == ResolveType.重考)
                {
                    physicalScore.SpecialType = SpecialScoreType.作废;
                    physicalScore.SetProperty("备注", "复议后作废");
                }
                else if (info.ResolveType == ResolveType.修改当前成绩)
                {
                    physicalScore.SpecialType = SpecialScoreType.作废;
                    physicalScore.SetProperty("备注", "复议后作废");
                    info.NewScoreItem.SetProperty("复议", info.ID);
                    info.NewScoreItem.SetProperty("备注", "复议后更正的成绩");
                    dc.GetTable<PhysicalScore>().InsertOnSubmit(info.NewScoreItem);
                }
            }
            physicalScore.SetProperty("复议", info.ID);
        }
    }
}
