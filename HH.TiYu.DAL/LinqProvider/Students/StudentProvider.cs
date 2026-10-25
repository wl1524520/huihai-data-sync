// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.GeneralLibrary.Core.DAL;
using HH.GeneralLibrary.Core.DAL.Linq;

namespace HH.TiYu.DAL.LinqProvider
{
    public class StudentProvider : ProviderBase<Student, string>, IBatchProvider<Student, string>, IProvider<Student, string>
    {
        public StudentProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        /// <summary>同步目标改绑检查；projectID 为空时检查全部年度。</summary>
        public bool HasLocalData(string projectID)
        {
            using (var dc = CreateDataContext())
                return dc.GetTable<Student>().Any(s => projectID == null || s.ProjectID == projectID) ||
                    dc.GetTable<StudentScore>().Any(s => projectID == null || s.ProjectID == projectID);
        }

        protected override Student GetingItemByID(string id, DataContext dc)
        {
            return dc.GetTable<Student>().SingleOrDefault((Student item) => item.ID == id);
        }

        protected override List<Student> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<Student> queryable = dc.GetTable<Student>();
            if (search is StudentSearchCondition)
            {
                StudentSearchCondition con = search as StudentSearchCondition;

                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    queryable = queryable.Where((Student item) => item.ProjectID == con.ProjectID);
                }
                if (con.StudentIDs != null && con.StudentIDs.Count > 0)
                {
                    queryable = queryable.Where((Student item) => con.StudentIDs.Contains(item.StudentID));
                }
            }
            return queryable.ToList();
        }

        // Deleting a roster entry must not discard already collected raw scores.
        public CommandResult BatchDelete(List<Student> students, string projectID)
        {
            try
            {
                using (var dc = CreateDataContext())
                {
                    var ids = students.Select(s => s.ID).Distinct().ToList();
                    var rows = dc.GetTable<Student>().Where(s => s.ProjectID == projectID && ids.Contains(s.ID)).ToList();
                    dc.GetTable<Student>().DeleteAllOnSubmit(rows);
                    dc.SubmitChanges();
                }
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex) { return new CommandResult(ResultCode.Fail, ex.Message); }
        }

        public CommandResult BatchAdd(List<Student> students, string projectID, ImportOption option)
        {
            try
            {
                using (var dc = CreateDataContext())
                {
                    var existing = dc.GetTable<Student>().Where(s => s.ProjectID == projectID).ToList()
                        .ToDictionary(s => s.StudentID, StringComparer.Ordinal);
                    foreach (var source in students)
                    {
                        if (string.IsNullOrWhiteSpace(source.StudentID) || source.StudentID.Length > 60)
                            throw new ArgumentException("学生 test_no 必须为 1 至 60 个字符");
                        Student target;
                        if (existing.TryGetValue(source.StudentID, out target))
                        {
                            source.ID = target.ID;
                            source.ProjectID = projectID;
                            if (option != ImportOption.Override) continue;
                        }
                        else
                        {
                            target = new Student { ID = string.IsNullOrEmpty(source.ID) ? Guid.NewGuid().ToString() : source.ID,
                                ProjectID = projectID, StudentID = source.StudentID };
                            dc.GetTable<Student>().InsertOnSubmit(target);
                            existing.Add(target.StudentID, target);
                            source.ID = target.ID;
                            source.ProjectID = projectID;
                        }
                        target.Name = source.Name;
                        target.Sex = source.Sex;
                        target.Grade = source.Grade;
                        target.ClassName = source.ClassName;
                        target.UpdateDate = DateTime.Now;
                    }
                    dc.SubmitChanges();
                }
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex) { return new CommandResult(ResultCode.Fail, ex.Message); }
        }

        public CommandResult BatchPatch(List<UpdateItem<string>> patchs)
        {
            try
            {
                string[] supported = { "Name", "Sex", "Grade", "ClassName", "UpdateDate" };
                if (patchs.Any(p => !supported.Contains(p.Key)))
                    throw new NotSupportedException("批量修改仅支持采集器实际存储的学生字段");
                using (var dc = CreateDataContext())
                {
                    var ids = patchs.Select(p => p.ID).Distinct().ToList();
                    var rows = dc.GetTable<Student>().Where(s => ids.Contains(s.ID)).ToDictionary(s => s.ID);
                    foreach (var patch in patchs)
                    {
                        Student student;
                        if (!rows.TryGetValue(patch.ID, out student)) continue;
                        switch (patch.Key)
                        {
                            case "Name": student.Name = patch.Value; break;
                            case "Sex": student.Sex = (Sex)Enum.Parse(typeof(Sex), patch.Value); break;
                            case "ClassName": student.ClassName = patch.Value; break;
                            case "Grade": student.Grade = string.IsNullOrEmpty(patch.Value) ? (int?)null : int.Parse(patch.Value); break;
                            case "UpdateDate": student.UpdateDate = string.IsNullOrEmpty(patch.Value) ? (DateTime?)null : DateTime.Parse(patch.Value); break;
                        }
                    }
                    dc.SubmitChanges();
                }
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex) { return new CommandResult(ResultCode.Fail, ex.Message); }
        }

        /// <summary>
        /// 平台名单同步的整页 upsert(单事务),按项目内 StudentID(去首尾空白、区分大小写)定位:
        ///   - 已存在:更新 StudentID(写回规范化值)/Name/Sex/ClassName/UpdateDate;Grade 仅在传入
        ///     非空时覆盖——平台年级越界时调用方传 null,即保留本地原年级,不让脏值覆盖好数据;
        ///   - 不存在:新增,Grade 原样写入(可为 null)。
        /// 返回(新增数, 更新数);失败抛异常,由调用方终止本轮同步。
        /// </summary>
        public Tuple<int, int> SyncRemotePage(List<Student> page, string projectID)
        {
            if (page == null) throw new ArgumentNullException("page");
            int inserted = 0, updated = 0;
            using (var dc = CreateDataContext())
            {
                var existing = new Dictionary<string, Student>(StringComparer.Ordinal);
                foreach (var row in dc.GetTable<Student>().Where(s => s.ProjectID == projectID).ToList())
                {
                    string key = (row.StudentID ?? string.Empty).Trim();
                    if (!existing.ContainsKey(key)) existing.Add(key, row);
                }
                foreach (var source in page)
                {
                    string testNo = (source.StudentID ?? string.Empty).Trim();
                    if (testNo.Length == 0 || testNo.Length > 60)
                        throw new ArgumentException("学生 test_no 必须为 1 至 60 个字符");
                    Student target;
                    if (existing.TryGetValue(testNo, out target))
                    {
                        target.StudentID = testNo;
                        target.Name = source.Name;
                        target.Sex = source.Sex;
                        if (source.Grade.HasValue) target.Grade = source.Grade;
                        target.ClassName = source.ClassName;
                        target.UpdateDate = DateTime.Now;
                        updated++;
                    }
                    else
                    {
                        target = new Student
                        {
                            ID = string.IsNullOrEmpty(source.ID) ? Guid.NewGuid().ToString() : source.ID,
                            ProjectID = projectID,
                            StudentID = testNo,
                            Name = source.Name,
                            Sex = source.Sex,
                            Grade = source.Grade,
                            ClassName = source.ClassName,
                            UpdateDate = DateTime.Now,
                        };
                        dc.GetTable<Student>().InsertOnSubmit(target);
                        existing.Add(testNo, target);
                        inserted++;
                    }
                }
                dc.SubmitChanges();
            }
            return Tuple.Create(inserted, updated);
        }
    }
}
