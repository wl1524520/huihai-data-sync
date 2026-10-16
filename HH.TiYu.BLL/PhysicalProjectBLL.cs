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
        private const ProjectLoadOptions 已移除加载项 = ProjectLoadOptions.学生成绩 | ProjectLoadOptions.评分标准 | ProjectLoadOptions.运动会分组
            | ProjectLoadOptions.综合评价 | ProjectLoadOptions.运动建议 | ProjectLoadOptions.学生分组;

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

        public void GetFullInfo(PhysicalProject project, ProjectLoadOptions items)
        {
            if ((items & 已移除加载项) > (ProjectLoadOptions)0)
            {
                throw new NotSupportedException("采集器不再支持以下项目加载项: 正式学生成绩、评分标准、运动会分组、综合评价、运动建议、学生分组");
            }
            if ((items & ProjectLoadOptions.区域信息) > (ProjectLoadOptions)0)
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
            if ((items & ProjectLoadOptions.学校信息) > (ProjectLoadOptions)0)
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
            if ((items & ProjectLoadOptions.学生信息) > (ProjectLoadOptions)0)
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
            if ((items & ProjectLoadOptions.测试科目) > (ProjectLoadOptions)0)
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

        public CommandResult SavePhysicalItems(PhysicalProject project, Sex sex, List<ProjectPhysicalItem> items)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<ProjectPhysicalItem, Guid>>(base.RepoUri).BatchAdd(items, project.ID, ImportOption.Override);
            if (commandResult.Result == ResultCode.Successful)
            {
                GetFullInfo(project, ProjectLoadOptions.测试科目);
            }
            return commandResult;
        }

        public QueryResultList<PhysicalProject> GetStudentPhysicalProjects(PhysicalProjectSearchCondition con)
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

    }
}
