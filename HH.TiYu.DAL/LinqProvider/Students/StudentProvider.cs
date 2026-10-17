// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;

namespace HH.TiYu.DAL.LinqProvider
{
    public class StudentProvider : ProviderBase<Student, Guid>, IBatchProvider<Student, Guid>, IProvider<Student, Guid>
    {
        public StudentProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override Student GetingItemByID(Guid id, DataContext dc)
        {
            return dc.GetTable<Student>().SingleOrDefault((Student item) => item.ID == id);
        }

        protected override List<Student> GetingItems(DataContext dc, SearchCondition search)
        {
            IQueryable<Student> queryable = dc.GetTable<Student>();
            if (search is StudentSearchCondition)
            {
                StudentSearchCondition con = search as StudentSearchCondition;
                if (!string.IsNullOrEmpty(con.School))
                {
                    queryable = queryable.Where((Student item) => item.FacilityID == con.School);
                }
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
                        .ToDictionary(s => s.StudentID, StringComparer.OrdinalIgnoreCase);
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
                            target = new Student { ID = source.ID == Guid.Empty ? Guid.NewGuid() : source.ID,
                                ProjectID = projectID, StudentID = source.StudentID };
                            dc.GetTable<Student>().InsertOnSubmit(target);
                            existing.Add(target.StudentID, target);
                            source.ID = target.ID;
                            source.ProjectID = projectID;
                        }
                        target.Name = source.Name;
                        target.Sex = source.Sex;
                        target.FacilityID = source.FacilityID;
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

        public CommandResult BatchPatch(List<UpdateItem<Guid>> patchs)
        {
            try
            {
                string[] supported = { "Name", "Sex", "FacilityID", "Grade", "ClassName", "UpdateDate" };
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
                            case "FacilityID": student.FacilityID = patch.Value; break;
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
    }
}
