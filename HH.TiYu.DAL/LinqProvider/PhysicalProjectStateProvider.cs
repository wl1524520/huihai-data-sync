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
    public class PhysicalProjectStateProvider : ProviderBase<PhysicalProjectState, string>
    {
        public PhysicalProjectStateProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        private void GetFullInfo(PhysicalProject project, 项目加载项 items)
        {
            if ((items & 项目加载项.区域信息) > (项目加载项)0)
            {
                if (project.AllDivisions == null)
                {
                    project.AllDivisions = new List<Division>();
                }
                QueryResultList<Division> items2 = new DivisionProvider(base.SqlURI, _MappingResource).GetItems(new DivisionSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items2.Result == ResultCode.Successful)
                {
                    project.AllDivisions.Clear();
                    project.AllDivisions.AddRange(items2.QueryObjects);
                }
            }
            if ((items & 项目加载项.学校信息) > (项目加载项)0)
            {
                if (project.AllFacilities == null)
                {
                    project.AllFacilities = new List<Facility>();
                }
                QueryResultList<Facility> items3 = new FacilityProvider(base.SqlURI, _MappingResource).GetItems(new FacilitySearchCondition
                {
                    ProjectID = project.ID
                });
                if (items3.Result == ResultCode.Successful)
                {
                    project.AllFacilities.Clear();
                    project.AllFacilities.AddRange(items3.QueryObjects);
                }
            }
            if ((items & 项目加载项.学生信息) > (项目加载项)0)
            {
                QueryResultList<Student> items4 = new StudentProvider(base.SqlURI, _MappingResource).GetItems(new StudentSearchCondition
                {
                    ProjectID = project.ID,
                    Graduation = false
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
                QueryResultList<PhysicalScore> items5 = new PhysicalScoreProvider(base.SqlURI, _MappingResource).GetItems(new PhysicalScoreSearchCondition
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
                QueryResultList<PhysicalItem> items6 = new PhysicalItemProvider(base.SqlURI, _MappingResource).GetItems(null);
                if (items6.Result == ResultCode.Successful)
                {
                    project.AllPhysicalItems.Clear();
                    project.AllPhysicalItems.AddRange(items6.QueryObjects);
                }
                project.PhysicalItems = new ProjectPhysicalItemProvider(base.SqlURI, _MappingResource).GetItems(new ProjectPhysicalItemSearchCondition
                {
                    ProjectID = project.ID
                }).QueryObjects;
            }
            if ((items & 项目加载项.学生分组) > (项目加载项)0)
            {
                QueryResultList<StudentGroup> items7 = new StudentGroupProvider(base.SqlURI, _MappingResource).GetItems(new StudentGroupSearchCondition
                {
                    ProjectID = project.ID
                });
                if (items7.Result == ResultCode.Successful)
                {
                    project.ClearPairs();
                    project.AddGroups(items7.QueryObjects);
                }
            }
        }

        private PhysicalProjectState GetState(PhysicalProject project)
        {
            Dictionary<string, Student> allStudents = project.AllStudents;
            PhysicalProjectState physicalProjectState = new PhysicalProjectState
            {
                ID = project.ID,
                Name = project.Name,
                Class = project.Class
            };
            if (allStudents != null && allStudents.Count > 0)
            {
                foreach (Student value in allStudents.Values)
                {
                    physicalProjectState.TotalCount++;
                    if (project.完成所有测试科目_中考(value.StudentID))
                    {
                        physicalProjectState.Completed++;
                        List<PhysicalScore> scoresAll = project.GetScoresAll(value.StudentID, null, 0, DateTime.Today);
                        if (scoresAll != null && scoresAll.Count > 0)
                        {
                            physicalProjectState.TodayCompleted++;
                        }
                    }
                    if (value.考试状态 != StudentState.正常考试 && value.考试状态 != StudentState.择考 && value.考试状态 != StudentState.回往生)
                    {
                        physicalProjectState.Absent++;
                    }
                    List<StudentGroupPair> pairsOfStudentID = project.GetPairsOfStudentID(value.StudentID);
                    if (pairsOfStudentID != null && pairsOfStudentID.Count > 0 && pairsOfStudentID.Any((StudentGroupPair it) => it.State == 1))
                    {
                        physicalProjectState.Checked++;
                        if (pairsOfStudentID.Any((StudentGroupPair it) => it.CheckTime.HasValue && it.CheckTime.Value.Date == DateTime.Today))
                        {
                            physicalProjectState.TodayChecked++;
                        }
                    }
                }
            }
            return physicalProjectState;
        }

        protected override PhysicalProjectState GetingItemByID(string id, DataContext dc)
        {
            PhysicalProject physicalProject = dc.GetTable<PhysicalProject>().SingleOrDefault((PhysicalProject item) => item.ID == id);
            if (physicalProject != null)
            {
                GetFullInfo(physicalProject, 项目加载项.区域学校学生信息 | 项目加载项.学生成绩 | 项目加载项.测试科目 | 项目加载项.学生分组);
                return GetState(physicalProject);
            }
            return null;
        }

        protected override List<PhysicalProjectState> GetingItems(DataContext dc, SearchCondition search)
        {
            List<PhysicalProject> queryObjects = new PhysicalProjectProvider(base.SqlURI, _MappingResource).GetItems(search).QueryObjects;
            if (queryObjects != null && queryObjects.Count > 0)
            {
                foreach (PhysicalProject item in queryObjects)
                {
                    GetFullInfo(item, 项目加载项.区域学校学生信息 | 项目加载项.学生成绩 | 项目加载项.测试科目 | 项目加载项.学生分组);
                }
                return queryObjects.Select((PhysicalProject it) => GetState(it)).ToList();
            }
            return null;
        }
    }
}
