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
    public class PhysicalProjectBLL : BLLBase<string, PhysicalProject>
    {
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
            if ((items & 项目加载项.学生成绩) > (项目加载项)0)
            {
                QueryResultList<PhysicalScore> items5 = new PhysicalScoreBLL(base.RepoUri).GetItems(new PhysicalScoreSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items5.Result == ResultCode.Successful)
                {
                    project.ClearAllTotals();
                    project.ClearScores();
                    List<PhysicalScore> queryObjects2 = items5.QueryObjects;
                    if (queryObjects2 != null && queryObjects2.Count > 0)
                    {
                        queryObjects2.ForEach(delegate(PhysicalScore it)
                        {
                            project.AddScore(it);
                        });
                    }
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
            if ((items & 项目加载项.评分标准) > (项目加载项)0)
            {
                if (project.AllStandards == null)
                {
                    project.AllStandards = new List<Standard>();
                }
                QueryResultList<Standard> items7 = new StandardBLL(base.RepoUri).GetItems(new StandardSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items7.Result == ResultCode.Successful)
                {
                    project.AllStandards.Clear();
                    project.AllStandards.AddRange(items7.QueryObjects);
                    project.AllStandards.ForEach(delegate(Standard it)
                    {
                        it.PhysicalItem = project.AllPhysicalItems.SingleOrDefault((PhysicalItem p) => p.ID == it.PhysicalItemID);
                    });
                }
            }
            if ((items & 项目加载项.运动会分组) > (项目加载项)0)
            {
                if (project.AllSports == null)
                {
                    project.AllSports = new List<Sports>();
                }
                project.AllSports.Clear();
                project.AllSports.AddRange(new SportsBLL(base.RepoUri).GetItems(null).QueryObjects);
            }
            if ((items & 项目加载项.综合评价) > (项目加载项)0)
            {
                if (project.AllAppraiseTemplates == null)
                {
                    project.AllAppraiseTemplates = new List<AppraiseTemplate>();
                }
                QueryResultList<AppraiseTemplate> items8 = new AppraiseTemplateBLL(base.RepoUri).GetItems(new AppraiseTemplateSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items8.Result == ResultCode.Successful)
                {
                    project.AllAppraiseTemplates.Clear();
                    project.AllAppraiseTemplates.AddRange(items8.QueryObjects);
                }
            }
            if ((items & 项目加载项.运动建议) > (项目加载项)0)
            {
                if (project.AllSportSuggestions == null)
                {
                    project.AllSportSuggestions = new List<SportSuggestion>();
                }
                QueryResultList<SportSuggestion> items9 = new SportSuggestionBLL(base.RepoUri).GetItems(new SportSuggestionSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items9.Result == ResultCode.Successful)
                {
                    project.AllSportSuggestions.Clear();
                    project.AllSportSuggestions.AddRange(items9.QueryObjects);
                }
            }
            if ((items & 项目加载项.学生分组) > (项目加载项)0)
            {
                QueryResultList<StudentGroup> items10 = new StudentGroupBLL(base.RepoUri).GetItems(new StudentGroupSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items10.Result == ResultCode.Successful)
                {
                    project.ClearPairs();
                    project.AddGroups(items10.QueryObjects);
                }
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
            QueryResultList<PhysicalScore> items2 = new PhysicalScoreBLL(base.RepoUri).GetItems(new PhysicalScoreSearchCondition
            {
                ProjectID = project.ID,
                StudentID = sid
            });
            if (items2.Result != ResultCode.Successful)
            {
                return;
            }
            project.ClearAllTotals();
            project.ClearScores();
            List<PhysicalScore> queryObjects2 = items2.QueryObjects;
            if (queryObjects2 != null && queryObjects2.Count > 0)
            {
                queryObjects2.ForEach(delegate(PhysicalScore it)
                {
                    project.AddScore(it);
                });
            }
        }

        public CommandResult AddScores(PhysicalProject project, List<PhysicalScore> scores, ImportOption option)
        {
            scores.ForEach(delegate(PhysicalScore it)
            {
                project.CalResult(project.GetStudentByID(it.StudentID), it);
            });
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchAdd(scores, project.ID, option);
            if (commandResult.Result == ResultCode.Successful)
            {
                scores.ForEach(delegate(PhysicalScore it)
                {
                    project.AddScore(it);
                });
                project.ClearAllTotals();
            }
            return commandResult;
        }

        public CommandResult UpdateScore(PhysicalProject project, PhysicalScore score)
        {
            Student studentByID = project.GetStudentByID(score.StudentID);
            if (studentByID == null)
            {
                return new CommandResult(ResultCode.Fail, "系统中不存在此学生");
            }
            project.CalResult(studentByID, score);
            CommandResult commandResult = new PhysicalScoreBLL(base.RepoUri).Update(score);
            if (commandResult.Result == ResultCode.Successful)
            {
                project.AddScore(score);
            }
            return commandResult;
        }

        public CommandResult DeleteScores(PhysicalProject project, List<PhysicalScore> scores, bool log = true)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchDelete(scores, project.ID);
            if (commandResult.Result == ResultCode.Successful)
            {
                List<Alarm> list = new List<Alarm>();
                foreach (PhysicalScore item in scores)
                {
                    if (log)
                    {
                        Student studentByID = project.GetStudentByID(item.StudentID);
                        PhysicalItem physicalItem = ((project.AllPhysicalItems != null) ? project.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == item.PhysicalItem) : null);
                        if (studentByID != null && physicalItem != null)
                        {
                            string alarmDescr = string.Format("删除学生 {{学号:{0} 姓名:{1}}} 的 {2} 成绩，原成绩 {3}", studentByID.StudentID, studentByID.Name, physicalItem.Name, physicalItem.ConvertToStr(item.Score));
                            Alarm item2 = new Alarm
                            {
                                ID = Guid.NewGuid(),
                                AlarmDateTime = DateTime.Now,
                                AlarmType = AlarmType.删除成绩,
                                OperatorID = Operator.Current.Name,
                                AlarmDescr = alarmDescr,
                                ProjectID = project.ID
                            };
                            list.Add(item2);
                        }
                    }
                }
                ProviderFactory.Create<IBatchProvider<Alarm, Guid>>(base.RepoUri).BatchAdd(list, project.ID, ImportOption.Append);
                scores.ForEach(delegate(PhysicalScore it)
                {
                    project.RemoveScore(it);
                });
                project.ClearAllTotals();
            }
            return commandResult;
        }

        public void CalScores(PhysicalProject project, List<Student> students, DateTime? dt = null)
        {
            try
            {
                List<PhysicalScore> list = new List<PhysicalScore>();
                List<PhysicalScore> list2 = new List<PhysicalScore>();
                List<PhysicalScore> list3 = new List<PhysicalScore>();
                foreach (Student student in students)
                {
                    ProjectPhysicalItem projectPhysicalItem = project.GetProjectPhysicalItem(student.Grade, student.Sex);
                    if (projectPhysicalItem == null)
                    {
                        continue;
                    }
                    foreach (PhysicalItem allPhysicalItem in project.AllPhysicalItems)
                    {
                        if ((project.Class != PhysicalProjectClass.学生体测 || allPhysicalItem.ID != PhysicalItem._BMI) && projectPhysicalItem.Contain(allPhysicalItem.ID))
                        {
                            CalScores(project, allPhysicalItem, student, list, list2, list3, dt);
                        }
                    }
                }
                if (list.Count > 0)
                {
                    AddScores(project, list, ImportOption.Append);
                }
                if (list3.Count > 0)
                {
                    DeleteScores(project, list3, false);
                }
                if (list2.Count > 0)
                {
                    List<UpdateItem<Guid>> list4 = new List<UpdateItem<Guid>>();
                    list4.AddRange(list2.Select((PhysicalScore it) => new UpdateItem<Guid>
                    {
                        ID = it.ID,
                        Key = "Result",
                        Value = (it.Result.HasValue ? it.Result.Value.ToString() : "null")
                    }));
                    list4.AddRange(list2.Select((PhysicalScore it) => new UpdateItem<Guid>
                    {
                        ID = it.ID,
                        Key = "Rank",
                        Value = it.Rank
                    }));
                    list4.AddRange(list2.Select((PhysicalScore it) => new UpdateItem<Guid>
                    {
                        ID = it.ID,
                        Key = "Jiafen",
                        Value = (it.Jiafen.HasValue ? it.Jiafen.Value.ToString() : "null")
                    }));
                    ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(list4);
                    project.ClearAllTotals();
                }
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
            }
        }

        private void CalScores(PhysicalProject project, PhysicalItem pi, Student s, List<PhysicalScore> addings, List<PhysicalScore> updatings, List<PhysicalScore> deletings, DateTime? dt = null)
        {
            if (!string.IsNullOrEmpty(pi.Formula))
            {
                if (pi.ID == PhysicalItem._BMI)
                {
                    List<PhysicalScore> scoresAll = project.GetScoresAll(s.StudentID, PhysicalItem._身高, 0, dt);
                    List<PhysicalScore> scoresAll2 = project.GetScoresAll(s.StudentID, PhysicalItem._体重, 0, dt);
                    if (scoresAll == null || scoresAll.Count == 0 || scoresAll2 == null || scoresAll2.Count == 0)
                    {
                        return;
                    }
                }
                string formula = project.Extra(s.StudentID, pi.Formula, null, 0, dt);
                decimal ret = default(decimal);
                if (!project.CalExpression(formula, out ret))
                {
                    return;
                }
                List<PhysicalScore> scoresAll3 = project.GetScoresAll(s.StudentID, pi.ID, 0, dt);
                PhysicalScore physicalScore = null;
                if (scoresAll3 != null && scoresAll3.Count > 0)
                {
                    foreach (PhysicalScore item in scoresAll3)
                    {
                        if (item.Score != ret)
                        {
                            deletings.Add(item);
                            continue;
                        }
                        PhysicalScore physicalScore2 = item.Clone();
                        project.CalResult(s, item);
                        if (!(item.Result == physicalScore2.Result) || item.Rank != physicalScore2.Rank)
                        {
                            deletings.Add(item);
                        }
                        else
                        {
                            physicalScore = item;
                        }
                    }
                }
                if (physicalScore == null)
                {
                    physicalScore = new PhysicalScore
                    {
                        ID = Guid.NewGuid(),
                        ProjectID = project.ID,
                        StudentID = s.StudentID,
                        PhysicalItem = pi.ID,
                        Grade = s.Grade,
                        UpdateDate = (dt.HasValue ? dt.Value : DateTime.Now),
                        Score = ret
                    };
                    project.CalResult(s, physicalScore);
                    addings.Add(physicalScore);
                }
                return;
            }
            List<PhysicalScore> scoresAll4 = project.GetScoresAll(s.StudentID, pi.ID, 0, dt);
            if (scoresAll4 == null || scoresAll4.Count <= 0)
            {
                return;
            }
            foreach (PhysicalScore item2 in scoresAll4)
            {
                PhysicalScore physicalScore3 = item2.Clone();
                project.CalResult(s, item2);
                if (!(item2.Result == physicalScore3.Result) || item2.Rank != physicalScore3.Rank || !(item2.Jiafen == physicalScore3.Jiafen))
                {
                    updatings.Add(item2);
                }
            }
        }

        public CommandResult SaveStandards(PhysicalProject project, List<Standard> items)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Standard, Guid>>(base.RepoUri).BatchAdd(items, project.ID, ImportOption.Override);
            if (commandResult.Result == ResultCode.Successful)
            {
                GetFullInfo(project, 项目加载项.评分标准);
            }
            return commandResult;
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

        public QueryResult<PhysicalProjectState> GetStateByID(string id)
        {
            return ProviderFactory.Create<IProvider<PhysicalProjectState, string>>(base.RepoUri).GetByID(id);
        }

        public QueryResultList<PhysicalProjectState> GetStates(SearchCondition con)
        {
            return ProviderFactory.Create<IProvider<PhysicalProjectState, string>>(base.RepoUri).GetItems(con);
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
