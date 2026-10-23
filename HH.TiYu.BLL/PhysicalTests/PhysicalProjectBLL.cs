// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Linq;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.BLL
{
    public class PhysicalProjectBLL : BLLBase<string, PhysicalProject>
    {
        /// <summary>本轮精简已移除的加载项；调用方传入即明确报不支持，避免静默成功后拿不到数据。</summary>
        private const ProjectLoadOptions RemovedLoadOptions = ProjectLoadOptions.StudentScores | ProjectLoadOptions.ScoringStandards | ProjectLoadOptions.SportsMeetGroups
            | ProjectLoadOptions.OverallEvaluations | ProjectLoadOptions.ExerciseSuggestions | ProjectLoadOptions.StudentGroups;

        public PhysicalProjectBLL(string repoUri)
            : base(repoUri)
        {
        }

        public override CommandResult Delete(PhysicalProject info)
        {
            if (info.ID == PhysicalProject.DefaultNationalFitnessProjectId || info.ID == PhysicalProject.DefaultStudentFitnessProjectId || info.ID == PhysicalProject.DefaultEntranceExamProjectId)
            {
                return new CommandResult(ResultCode.Fail, "系统自建的项目不能删除");
            }
            return base.Delete(info);
        }

        public void GetFullInfo(PhysicalProject project, ProjectLoadOptions items)
        {
            if ((items & RemovedLoadOptions) > (ProjectLoadOptions)0)
            {
                throw new NotSupportedException("采集器不再支持以下项目加载项: 正式学生成绩、评分标准、运动会分组、综合评价、运动建议、学生分组");
            }
            if ((items & ProjectLoadOptions.Facilities) > (ProjectLoadOptions)0)
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
            if ((items & ProjectLoadOptions.Students) > (ProjectLoadOptions)0)
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
            if ((items & ProjectLoadOptions.PhysicalItems) > (ProjectLoadOptions)0)
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
                project.PhysicalItems = ProviderFactory.Create<IProvider<ProjectPhysicalItem, string>>(base.RepoUri).GetItems(new ProjectPhysicalItemSearchCondition
                {
                    ProjectID = project.ID
                }).QueryObjects;
            }
        }

        public CommandResult SavePhysicalItems(PhysicalProject project, Sex sex, List<ProjectPhysicalItem> items)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<ProjectPhysicalItem, string>>(base.RepoUri).BatchAdd(items, project.ID, ImportOption.Override);
            if (commandResult.Result == ResultCode.Successful)
            {
                GetFullInfo(project, ProjectLoadOptions.PhysicalItems);
            }
            return commandResult;
        }

        public QueryResultList<PhysicalProject> GetStudentPhysicalProjects(PhysicalProjectSearchCondition con)
        {
            if (con != null)
            {
                con.Class = PhysicalProjectClass.StudentFitness;
            }
            else
            {
                con = new PhysicalProjectSearchCondition
                {
                    Class = PhysicalProjectClass.StudentFitness
                };
            }
            QueryResultList<PhysicalProject> items = new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetItems(con);
            if (items.QueryObjects != null && items.QueryObjects.Count > 0)
            {
                items.QueryObjects = (from item in items.QueryObjects
                    where item.ID != PhysicalProject.DefaultStudentFitnessProjectId && item.ID != PhysicalProject.DefaultEntranceExamProjectId && item.ID != PhysicalProject.DefaultNationalFitnessProjectId && item.Name.Contains("年度学生体测")
                    orderby item.Name descending
                    select item).ToList();
            }
            return items;
        }

    }
}
