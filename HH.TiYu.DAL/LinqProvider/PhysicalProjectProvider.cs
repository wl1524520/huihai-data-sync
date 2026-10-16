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
    public class PhysicalProjectProvider : ProviderBase<PhysicalProject, string>
    {
        public PhysicalProjectProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        private void CopyInfo(PhysicalProject project, string source, 项目加载项 items, DataContext dc)
        {
            if ((items & 项目加载项.测试科目) > (项目加载项)0)
            {
                List<ProjectPhysicalItem> list = (from projectPhysicalItem2 in dc.GetTable<ProjectPhysicalItem>()
                    where projectPhysicalItem2.ProjectID == source
                    select projectPhysicalItem2).ToList();
                foreach (ProjectPhysicalItem item in list)
                {
                    ProjectPhysicalItem projectPhysicalItem = item.Clone();
                    projectPhysicalItem.ID = Guid.NewGuid();
                    projectPhysicalItem.ProjectID = project.ID;
                    dc.GetTable<ProjectPhysicalItem>().InsertOnSubmit(projectPhysicalItem);
                }
            }
            if ((items & 项目加载项.评分标准) > (项目加载项)0)
            {
                List<Standard> list2 = (from standard2 in dc.GetTable<Standard>()
                    where standard2.ProjectID == source
                    select standard2).ToList();
                List<StandardItem> source2 = (from s in dc.GetTable<Standard>()
                    join si in dc.GetTable<StandardItem>() on s.ID equals si.StandardID
                    where s.ProjectID == source
                    select si).ToList();
                foreach (Standard it in list2)
                {
                    Standard standard = it.Clone();
                    standard.ID = Guid.NewGuid();
                    standard.ProjectID = project.ID;
                    List<StandardItem> list3 = source2.Where((StandardItem si) => si.StandardID == it.ID).ToList();
                    foreach (StandardItem item2 in list3)
                    {
                        StandardItem standardItem = item2.Clone();
                        standardItem.ID = Guid.NewGuid();
                        standardItem.StandardID = standard.ID;
                        standard.Items.Add(standardItem);
                    }
                    dc.GetTable<Standard>().InsertOnSubmit(standard);
                }
            }
            if ((items & 项目加载项.综合评价) > (项目加载项)0)
            {
                List<AppraiseTemplate> list4 = (from appraiseTemplate2 in dc.GetTable<AppraiseTemplate>()
                    where appraiseTemplate2.ProjectID == source
                    select appraiseTemplate2).ToList();
                foreach (AppraiseTemplate item3 in list4)
                {
                    AppraiseTemplate appraiseTemplate = item3.Clone();
                    appraiseTemplate.ID = Guid.NewGuid();
                    appraiseTemplate.ProjectID = project.ID;
                    dc.GetTable<AppraiseTemplate>().InsertOnSubmit(appraiseTemplate);
                }
            }
            if ((items & 项目加载项.运动建议) <= (项目加载项)0)
            {
                return;
            }
            List<SportSuggestion> list5 = (from sportSuggestion2 in dc.GetTable<SportSuggestion>()
                where sportSuggestion2.ProjectID == source
                select sportSuggestion2).ToList();
            foreach (SportSuggestion item4 in list5)
            {
                SportSuggestion sportSuggestion = item4.Clone();
                sportSuggestion.ID = Guid.NewGuid();
                sportSuggestion.ProjectID = project.ID;
                dc.GetTable<SportSuggestion>().InsertOnSubmit(sportSuggestion);
            }
        }

        protected override PhysicalProject GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<PhysicalProject>().SingleOrDefault((PhysicalProject item) => item.ID == id);
        }

        protected override List<PhysicalProject> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<PhysicalProject> source = dc.GetTable<PhysicalProject>();
            if (search is PhysicalProjectSearchCondition)
            {
                PhysicalProjectSearchCondition con = search as PhysicalProjectSearchCondition;
                if (con.Class.HasValue)
                {
                    source = source.Where((PhysicalProject it) => (int)it.Class == (int)con.Class.Value);
                }
                if (!string.IsNullOrEmpty(con.Name))
                {
                    source = source.Where((PhysicalProject it) => it.Name == con.Name);
                }
                if (con.IDs != null && con.IDs.Count > 0)
                {
                    source = source.Where((PhysicalProject it) => con.IDs.Contains(it.ID));
                }
                if (!string.IsNullOrEmpty(con.Exclude))
                {
                    source = source.Where((PhysicalProject it) => it.ID != con.Exclude);
                }
            }
            return source.ToList();
        }

        protected override void InsertingItem(PhysicalProject info, DataContext dc)
        {
            if (string.IsNullOrEmpty(info.ID))
            {
                info.ID = new AutoNumberCreater(base.SqlURI, _MappingResource).CreateNumber("P", 4, "PhysicalProject");
            }
            if (string.IsNullOrEmpty(info.ID))
            {
                throw new Exception("创建编号失败");
            }
            if (!string.IsNullOrEmpty(info.TemplateProjectID))
            {
                CopyInfo(info, info.TemplateProjectID, 项目加载项.测试科目 | 项目加载项.评分标准 | 项目加载项.综合评价 | 项目加载项.运动建议, dc);
            }
            base.InsertingItem(info, dc);
        }

        protected override void DeletingItem(PhysicalProject info, DataContext dc)
        {
            string command = string.Format("DELETE from AlarmInfo where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from AppraiseTemplate where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from  Division where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from Facility where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from PhysicalScore where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from ProjectPhysicalItems where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from SportSuggestion where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from Standard where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from StandardItem where not exists (select * from Standard where StandardItem.StandardID=Standard.ID) ");
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from Student where ProjectID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from PhysicalProject where ID='{0}' ", info.ID);
            dc.ExecuteCommand(command);
            base.DeletingItem(info, dc);
        }
    }
}
