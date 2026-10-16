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
                if (con.Graduation.HasValue)
                {
                    queryable = ((!con.Graduation.Value) ? queryable.Where((Student item) => item.Graduation == (int?)null) : queryable.Where((Student item) => item.Graduation != (int?)null));
                }
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    queryable = queryable.Where((Student item) => item.ProjectID == con.ProjectID);
                }
                if (con.StudentIDs != null && con.StudentIDs.Count > 0)
                {
                    queryable = queryable.Where((Student item) => con.StudentIDs.Contains(item.StudentID));
                }
                if (!string.IsNullOrEmpty(con.CardID))
                {
                    queryable = queryable.Where((Student item) => item.CardID == con.CardID);
                }
            }
            // 采集器不支持照片：不查询 StudentPhoto，HasPhoto 保持默认空值。
            return queryable.ToList();
        }

        protected override void InsertingItem(Student info, DataContext dc)
        {
            if (!string.IsNullOrEmpty(info.CardID) && dc.GetTable<Student>().Count((Student it) => it.CardID == info.CardID && it.ProjectID == info.ProjectID && it.ID != info.ID) > 0)
            {
                throw new Exception("学生的卡号已经被其它学生使用");
            }
            base.InsertingItem(info, dc);
        }

        protected override void UpdatingItem(Student newVal, Student original, DataContext dc)
        {
            if (!string.IsNullOrEmpty(newVal.CardID) && dc.GetTable<Student>().Count((Student it) => it.CardID == newVal.CardID && it.ProjectID == newVal.ProjectID && it.ID != newVal.ID) > 0)
            {
                throw new Exception("学生的卡号已经被其它学生使用");
            }
            base.UpdatingItem(newVal, original, dc);
        }

        protected override void DeletingItem(Student info, DataContext dc)
        {
            string studentID = info.StudentID;
            studentID = studentID.Replace("'", "''");
            string command = string.Format("DELETE from PhysicalScore where StudentID='{0}' and ProjectID='{1}'", studentID, info.ProjectID);
            dc.ExecuteCommand(command);
            command = string.Format("DELETE from StudentGroup where StudentID='{0}' and ProjectID='{1}'", studentID, info.ProjectID);
            dc.ExecuteCommand(command);
            base.DeletingItem(info, dc);
        }

        public CommandResult BatchAdd(List<Student> students, string projectID, ImportOption option)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                List<Student> list = (from it in dataContext.GetTable<Student>()
                    where it.ProjectID == projectID
                    select it).ToList();
                Dictionary<string, Student> dictionary = new Dictionary<string, Student>();
                Dictionary<string, Student> dictionary2 = new Dictionary<string, Student>();
                foreach (Student item in list)
                {
                    if (!dictionary.ContainsKey(item.StudentID.ToUpper()))
                    {
                        dictionary.Add(item.StudentID.ToUpper(), item);
                    }
                    if (!string.IsNullOrEmpty(item.CardID) && !dictionary2.ContainsKey(item.CardID))
                    {
                        dictionary2.Add(item.CardID, item);
                    }
                }
                foreach (Student student2 in students)
                {
                    if (!string.IsNullOrEmpty(student2.CardID) && dictionary2.ContainsKey(student2.CardID) && dictionary2[student2.CardID].StudentID != student2.StudentID)
                    {
                        continue;
                    }
                    Student student = (dictionary.ContainsKey(student2.StudentID.ToUpper()) ? dictionary[student2.StudentID.ToUpper()] : null);
                    if (student != null)
                    {
                        if (option == ImportOption.Override)
                        {
                            CopyValue(student2, student);
                        }
                        continue;
                    }
                    if (student2.ID == Guid.Empty)
                    {
                        student2.ID = Guid.NewGuid();
                    }
                    if (string.IsNullOrEmpty(student2.ProjectID) && PhysicalProject.Current != null)
                    {
                        student2.ProjectID = projectID;
                    }
                    dataContext.GetTable<Student>().InsertOnSubmit(student2);
                }
                dataContext.SubmitChanges();
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }

        private bool CopyValue(Student source, Student des)
        {
            bool flag = false;
            if (des.Name != source.Name)
            {
                des.Name = source.Name;
                flag = true;
            }
            if (des.Sex != source.Sex)
            {
                des.Sex = source.Sex;
                flag = true;
            }
            if (des.FacilityID != source.FacilityID)
            {
                des.FacilityID = source.FacilityID;
                flag = true;
            }
            if (des.Nation != source.Nation)
            {
                des.Nation = source.Nation;
                flag = true;
            }
            if (des.Grade != source.Grade)
            {
                des.Grade = source.Grade;
                flag = true;
            }
            if (des.BirthDay != source.BirthDay && source.BirthDay.HasValue)
            {
                des.BirthDay = source.BirthDay;
                flag = true;
            }
            if (des.ClassID != source.ClassID && !string.IsNullOrEmpty(source.ClassID))
            {
                des.ClassID = source.ClassID;
                flag = true;
            }
            if (des.ClassName != source.ClassName && !string.IsNullOrEmpty(source.ClassName))
            {
                des.ClassName = source.ClassName;
                flag = true;
            }
            if (des.IDNumber != source.IDNumber && !string.IsNullOrEmpty(source.IDNumber))
            {
                des.IDNumber = source.IDNumber;
                flag = true;
            }
            if (des.District != source.District && !string.IsNullOrEmpty(source.District))
            {
                des.District = source.District;
                flag = true;
            }
            if (des.Address != source.Address && !string.IsNullOrEmpty(source.Address))
            {
                des.Address = source.Address;
                flag = true;
            }
            if (des.CardID != source.CardID && !string.IsNullOrEmpty(source.CardID))
            {
                des.CardID = source.CardID;
                flag = true;
            }
            if (des.Note != source.Note && !string.IsNullOrEmpty(source.Note))
            {
                des.Note = source.Note;
                flag = true;
            }
            if (flag)
            {
                des.UpdateDate = DateTime.Now;
            }
            return flag;
        }

        public CommandResult BatchDelete(List<Student> students, string projectID)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                string arg = string.Join(",", students.Select((Student it) => string.Format("'{0}'", it.StudentID.Replace("'", "''"))));
                string command = string.Format("DELETE from Student where StudentID in ({0}) and ProjectID='{1}' \n", arg, projectID);
                dataContext.ExecuteCommand(command);
                command = string.Format("DELETE from PhysicalScore where StudentID in ({0}) and ProjectID='{1}' \n", arg, projectID);
                dataContext.ExecuteCommand(command);
                command = string.Format("DELETE from StudentGroupPairV2 where StudentID in ({0}) \n", arg, projectID);
                dataContext.ExecuteCommand(command);
                command = string.Format("DELETE from StudentGroupV2 where not exists (select * from StudentGroupPairV2 where StudentGroupPairV2.GroupID=StudentGroupV2.ID)  \n", arg, projectID);
                dataContext.ExecuteCommand(command);
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }

        public CommandResult BatchPatch(List<UpdateItem<Guid>> patchs)
        {
            try
            {
                List<Guid> ids = patchs.Select((UpdateItem<Guid> it) => it.ID).Distinct().ToList();
                DataContext dataContext = CreateDataContext();
                List<Student> list = (from it in dataContext.GetTable<Student>()
                    where ids.Contains(it.ID)
                    select it).ToList();
                Dictionary<Guid, Student> dictionary = new Dictionary<Guid, Student>();
                foreach (Student item in list)
                {
                    if (!dictionary.ContainsKey(item.ID))
                    {
                        dictionary.Add(item.ID, item);
                    }
                }
                foreach (UpdateItem<Guid> patch in patchs)
                {
                    if (dictionary.ContainsKey(patch.ID))
                    {
                        Student student = dictionary[patch.ID];
                        switch (patch.Key)
                        {
                        case "IsAbsent":
                            student.IsAbsent = ((!string.IsNullOrEmpty(patch.Value)) ? new bool?(bool.Parse(patch.Value)) : ((bool?)null));
                            break;
                        case "Graduation":
                            student.Graduation = (string.IsNullOrEmpty(patch.Value) ? ((int?)null) : new int?(int.Parse(patch.Value)));
                            break;
                        case "Grade":
                            student.Grade = (string.IsNullOrEmpty(patch.Value) ? ((int?)null) : new int?(int.Parse(patch.Value)));
                            break;
                        case "UpdateDate":
                            student.UpdateDate = (string.IsNullOrEmpty(patch.Value) ? ((DateTime?)null) : new DateTime?(DateTime.Parse(patch.Value)));
                            break;
                        case "Note":
                            student.Note = patch.Value;
                            break;
                        default:
                            student.SetProperty(patch.Key, patch.Value);
                            break;
                        }
                    }
                }
                dataContext.SubmitChanges();
                return new CommandResult(ResultCode.Successful, string.Empty);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
                return new CommandResult(ResultCode.Fail, ex.Message);
            }
        }
    }
}
