// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Linq;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using LJH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.BLL
{
    public class PhysicalProjectBLL : BLLBase<string, PhysicalProject>
    {
        /// <summary>本轮精简已移除的加载项；调用方传入即明确报不支持，避免静默成功后拿不到数据。</summary>
        private const 项目加载项 已移除加载项 = 项目加载项.学生成绩 | 项目加载项.评分标准 | 项目加载项.运动会分组
            | 项目加载项.综合评价 | 项目加载项.运动建议 | 项目加载项.学生分组;

        public PhysicalProjectBLL(string repoUri)
            : base(repoUri)
        {
        }

        public override CommandResult Delete(PhysicalProject info)
        {
            if (info.ID == PhysicalProject.默认国民体测项目 || info.ID == PhysicalProject.默认学生体测项目 || info.ID == PhysicalProject.默认中考项目)
            {
                return new CommandResult(ResultCode.Fail, "系统自建的项目不能删除");
            }
            return base.Delete(info);
        }

        public void GetFullInfo(PhysicalProject project, 项目加载项 items)
        {
            if ((items & 已移除加载项) > (项目加载项)0)
            {
                throw new NotSupportedException("采集器不再支持以下项目加载项: 正式学生成绩、评分标准、运动会分组、综合评价、运动建议、学生分组");
            }
            if ((items & 项目加载项.区域信息) > (项目加载项)0)
            {
                if (project.AllDivisions == null)
                {
                    project.AllDivisions = new List<Division>();
                }
                QueryResultList<Division> items2 = new DivisionBLL(base.RepoUri).GetItems(new DivisionSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items2.Result == ResultCode.Successful)
                {
                    project.AllDivisions.Clear();
                    project.AllDivisions.AddRange(items2.QueryObjects.OrderBy((Division it) => it.Name));
                }
            }
            if ((items & 项目加载项.学校信息) > (项目加载项)0)
            {
                if (project.AllFacilities == null)
                {
                    project.AllFacilities = new List<Facility>();
                }
                QueryResultList<Facility> items3 = new FacilityBLL(base.RepoUri).GetItems(new FacilitySearchCondition
                {
                    ProjectID = project.ID
                });
                if (items3.Result == ResultCode.Successful)
                {
                    project.AllFacilities.Clear();
                    List<Facility> list = items3.QueryObjects.OrderBy((Facility it) => it.Name).ToList();
                    project.AllFacilities.AddRange(items3.QueryObjects.OrderBy((Facility it) => it.Name));
                }
            }
            if ((items & 项目加载项.学生信息) > (项目加载项)0)
            {
                QueryResultList<Student> items4 = new StudentBLL(base.RepoUri).GetItems(new StudentSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items4.Result == ResultCode.Successful)
                {
                    project.ClearStudents();
                    List<Student> queryObjects = items4.QueryObjects;
                    project.AddStudents(queryObjects);
                }
            }
            if ((items & 项目加载项.测试科目) > (项目加载项)0)
            {
                if (project.AllPhysicalItems == null)
                {
                    project.AllPhysicalItems = new List<PhysicalItem>();
                }
                QueryResultList<PhysicalItem> items6 = new PhysicalItemBLL(base.RepoUri).GetItems(null);
                if (items6.Result == ResultCode.Successful)
                {
                    project.AllPhysicalItems.Clear();
                    project.AllPhysicalItems.AddRange(items6.QueryObjects);
                }
                if (project.AllPhysicalItems != null)
                {
                    PhysicalItem physicalItem = project.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == PhysicalItem._BMI);
                    if (physicalItem != null)
                    {
                        project.AllPhysicalItems.Remove(physicalItem);
                        project.AllPhysicalItems.Insert(2, physicalItem);
                    }
                }
                project.PhysicalItems = ProviderFactory.Create<IProvider<ProjectPhysicalItem, Guid>>(base.RepoUri).GetItems(new ProjectPhysicalItemSearchCondition
                {
                    ProjectID = project.ID
                }).QueryObjects;
            }
        }

        public void GetStudentInfo(PhysicalProject project, string sid)
        {
            QueryResultList<Student> items = new StudentBLL(base.RepoUri).GetItems(new StudentSearchCondition
            {
                ProjectID = project.ID,
                StudentIDs = new List<string> { sid }
            });
            if (items.Result == ResultCode.Successful)
            {
                project.ClearStudents();
                List<Student> queryObjects = items.QueryObjects;
                project.AddStudents(queryObjects);
            }
        }

        public CommandResult SavePhysicalItems(PhysicalProject project, Sex sex, List<ProjectPhysicalItem> items)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<ProjectPhysicalItem, Guid>>(base.RepoUri).BatchAdd(items, project.ID, ImportOption.Override);
            if (commandResult.Result == ResultCode.Successful)
            {
                GetFullInfo(project, 项目加载项.测试科目);
            }
            return commandResult;
        }

        public CommandResult<PhysicalProject> CreateOrGetProject_学生体测(int year)
        {
            PhysicalProject p = null;
            string name = string.Format("{0}年度学生体测", year);
            List<PhysicalProject> queryObjects = new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetItems(new PhysicalProjectSearchCondition
            {
                Class = PhysicalProjectClass.学生体测,
                Name = name
            }).QueryObjects;
            if (queryObjects != null && queryObjects.Count > 0)
            {
                p = queryObjects[0];
            }
            if (p != null && year >= 2019)
            {
                List<ProjectPhysicalItem> list = PhysicalProjectHelper.创建学生体测项目(Sex.Male);
                list.ForEach(delegate(ProjectPhysicalItem it)
                {
                    it.ProjectID = p.ID;
                });
                new PhysicalProjectBLL(AppSettings.Current.ConnStr).SavePhysicalItems(p, Sex.Male, list);
                list = PhysicalProjectHelper.创建学生体测项目(Sex.Female);
                list.ForEach(delegate(ProjectPhysicalItem it)
                {
                    it.ProjectID = p.ID;
                });
                new PhysicalProjectBLL(AppSettings.Current.ConnStr).SavePhysicalItems(p, Sex.Female, list);
            }
            if (p == null)
            {
                p = new PhysicalProject();
                p.Class = PhysicalProjectClass.学生体测;
                p.Name = name;
                p.CreateDate = DateTime.Now;
                p.StartDate = new DateTime(year, 8, 1);
                p.EndDate = p.StartDate.Value.AddYears(1);
                p.TemplateProjectID = PhysicalProject.默认学生体测项目;
                CommandResult commandResult = new PhysicalProjectBLL(AppSettings.Current.ConnStr).Add(p);
                if (commandResult.Result != ResultCode.Successful)
                {
                    return new CommandResult<PhysicalProject>(ResultCode.Fail, commandResult.Message, null);
                }
                if (year >= 2019)
                {
                    List<ProjectPhysicalItem> list2 = PhysicalProjectHelper.创建学生体测项目(Sex.Male);
                    list2.ForEach(delegate(ProjectPhysicalItem it)
                    {
                        it.ProjectID = p.ID;
                    });
                    new PhysicalProjectBLL(AppSettings.Current.ConnStr).SavePhysicalItems(p, Sex.Male, list2);
                    list2 = PhysicalProjectHelper.创建学生体测项目(Sex.Female);
                    list2.ForEach(delegate(ProjectPhysicalItem it)
                    {
                        it.ProjectID = p.ID;
                    });
                    new PhysicalProjectBLL(AppSettings.Current.ConnStr).SavePhysicalItems(p, Sex.Female, list2);
                }
            }
            return new CommandResult<PhysicalProject>(ResultCode.Successful, string.Empty, p);
        }

        public QueryResultList<PhysicalProject> 获取学生体测项目(PhysicalProjectSearchCondition con)
        {
            if (con != null)
            {
                con.Class = PhysicalProjectClass.学生体测;
            }
            else
            {
                con = new PhysicalProjectSearchCondition
                {
                    Class = PhysicalProjectClass.学生体测
                };
            }
            QueryResultList<PhysicalProject> items = new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetItems(con);
            if (items.QueryObjects != null && items.QueryObjects.Count > 0)
            {
                items.QueryObjects = (from item in items.QueryObjects
                    where item.ID != PhysicalProject.默认学生体测项目 && item.ID != PhysicalProject.默认中考项目 && item.ID != PhysicalProject.默认国民体测项目 && item.Name.Contains("年度学生体测")
                    orderby item.Name descending
                    select item).ToList();
            }
            return items;
        }

        public CommandResult<PhysicalProject> CreateOrGetProject_抽测(int year)
        {
            PhysicalProject physicalProject = null;
            string name = string.Format("{0}年度学生抽测", year);
            List<PhysicalProject> queryObjects = new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetItems(new PhysicalProjectSearchCondition
            {
                Class = PhysicalProjectClass.学生体测,
                Name = name
            }).QueryObjects;
            if (queryObjects != null && queryObjects.Count > 0)
            {
                physicalProject = queryObjects[0];
            }
            if (physicalProject == null)
            {
                physicalProject = new PhysicalProject();
                physicalProject.Class = PhysicalProjectClass.学生体测;
                physicalProject.Name = name;
                physicalProject.CreateDate = DateTime.Now;
                physicalProject.StartDate = new DateTime(year, 1, 1);
                physicalProject.EndDate = physicalProject.StartDate.Value.AddYears(1);
                physicalProject.TemplateProjectID = PhysicalProject.默认学生体测项目;
                CommandResult commandResult = new PhysicalProjectBLL(AppSettings.Current.ConnStr).Add(physicalProject);
                if (commandResult.Result != ResultCode.Successful)
                {
                    return new CommandResult<PhysicalProject>(ResultCode.Fail, commandResult.Message, null);
                }
            }
            return new CommandResult<PhysicalProject>(ResultCode.Successful, string.Empty, physicalProject);
        }

        public QueryResultList<PhysicalProject> 获取学生抽测项目(PhysicalProjectSearchCondition con)
        {
            if (con != null)
            {
                con.Class = PhysicalProjectClass.学生体测;
            }
            else
            {
                con = new PhysicalProjectSearchCondition
                {
                    Class = PhysicalProjectClass.学生体测
                };
            }
            QueryResultList<PhysicalProject> items = new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetItems(con);
            if (items.QueryObjects != null && items.QueryObjects.Count > 0)
            {
                items.QueryObjects = (from item in items.QueryObjects
                    where item.ID != PhysicalProject.默认学生体测项目 && item.ID != PhysicalProject.默认中考项目 && item.ID != PhysicalProject.默认国民体测项目 && item.Name.Contains("年度学生抽测")
                    orderby item.Name descending
                    select item).ToList();
            }
            return items;
        }
    }
}
