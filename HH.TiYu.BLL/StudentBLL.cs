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
    public class StudentBLL : BLLBase<Guid, Student>
    {
        public StudentBLL(string repoUri)
            : base(repoUri)
        {
        }

        public override CommandResult Add(Student info)
        {
            if (info.ID == Guid.Empty)
            {
                info.ID = Guid.NewGuid();
            }
            if (string.IsNullOrEmpty(info.ProjectID))
            {
                info.ProjectID = PhysicalProject.Current.ID;
            }
            CommandResult commandResult = base.Add(info);
            if (commandResult.Result == ResultCode.Successful)
            {
                PhysicalProject.Current.AddStudent(info);
            }
            return commandResult;
        }

        public CommandResult 更新考试科目(List<Student> students)
        {
            string key = StudentNote.考试科目.ToString();
            List<UpdateItem<Guid>> patchs = students.Select((Student it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = key,
                Value = it.GetProperty(key)
            }).ToList();
            return ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchPatch(patchs);
        }

        public CommandResult Upgrade(List<Student> students, int maxGrade)
        {
            CommandResult commandResult = null;
            List<UpdateItem<Guid>> list = new List<UpdateItem<Guid>>();
            DateTime now = DateTime.Now;
            foreach (Student student2 in students)
            {
                Student student = student2.Clone();
                student.Upgrade(maxGrade);
                if (student.Graduation != student2.Graduation)
                {
                    list.Add(new UpdateItem<Guid>
                    {
                        ID = student2.ID,
                        Key = "Graduation",
                        Value = (student.Graduation.HasValue ? student.Graduation.ToString() : "null")
                    });
                }
                if (student.Grade != student2.Grade)
                {
                    list.Add(new UpdateItem<Guid>
                    {
                        ID = student2.ID,
                        Key = "Grade",
                        Value = student.Grade.ToString()
                    });
                }
                list.Add(new UpdateItem<Guid>
                {
                    ID = student2.ID,
                    Key = "UpdateDate",
                    Value = now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchPatch(list);
            if (commandResult.Result == ResultCode.Successful)
            {
                foreach (Student student3 in students)
                {
                    student3.UpdateDate = now;
                    student3.Upgrade(maxGrade);
                }
            }
            return commandResult;
        }

        public CommandResult Absent(List<Student> students)
        {
            List<UpdateItem<Guid>> patchs = students.Select((Student it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = "IsAbsent",
                Value = true.ToString()
            }).ToList();
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchPatch(patchs);
            if (commandResult.Result == ResultCode.Successful)
            {
                foreach (Student student in students)
                {
                    student.IsAbsent = true;
                    student.UpdateDate = DateTime.Now;
                }
            }
            return commandResult;
        }

        public CommandResult UndoAbsent(List<Student> students)
        {
            List<UpdateItem<Guid>> patchs = students.Select((Student it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = "IsAbsent",
                Value = null
            }).ToList();
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchPatch(patchs);
            if (commandResult.Result == ResultCode.Successful)
            {
                foreach (Student student in students)
                {
                    student.IsAbsent = null;
                    student.UpdateDate = DateTime.Now;
                }
            }
            return commandResult;
        }

        public CommandResult UpdateUploadToCloudDate(ThirdSyncPlateForm pf, List<Student> students, DateTime dt)
        {
            List<UpdateItem<Guid>> patchs = students.Select((Student it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = pf.ToString(),
                Value = dt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchPatch(patchs);
            if (commandResult.Result == ResultCode.Successful)
            {
                foreach (Student student in students)
                {
                    student.设置上传到平台时间(pf, dt);
                }
            }
            return commandResult;
        }

        public CommandResult ClearUploadToCloudDate(ThirdSyncPlateForm pf, List<Student> ss)
        {
            List<UpdateItem<Guid>> list = ss.Select((Student it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = pf.ToString(),
                Value = null
            }).ToList();
            list.AddRange(ss.Select((Student it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = string.Format("{0}_msg", pf.ToString()),
                Value = null
            }).ToList());
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(list);
            if (commandResult.Result == ResultCode.Successful)
            {
                ss.ForEach(delegate(Student it)
                {
                    it.设置上传到平台时间(pf, null);
                    it.设置上传到平台错误信息(pf, null);
                });
            }
            return commandResult;
        }

        public CommandResult AddStudents(PhysicalProject project, List<Student> students, bool ignore)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchAdd(students, project.ID, (!ignore) ? ImportOption.Override : ImportOption.Ignore);
            if (commandResult.Result == ResultCode.Successful)
            {
                project.AddStudents(students);
            }
            return commandResult;
        }

        public CommandResult DeleteStudents(PhysicalProject project, List<Student> students)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchDelete(students, project.ID);
            if (commandResult.Result == ResultCode.Successful)
            {
                project.RemoveStudents(students);
                foreach (Student student in students)
                {
                    List<PhysicalScore> scoresAll = project.GetScoresAll(student.StudentID, null);
                    if (scoresAll != null && scoresAll.Count > 0)
                    {
                        scoresAll.ForEach(delegate(PhysicalScore it)
                        {
                            project.RemoveScore(it);
                        });
                    }
                }
            }
            return commandResult;
        }

        public CommandResult 设置学生特殊情况(List<Student> students, StudentState? spt)
        {
            List<UpdateItem<Guid>> patchs = students.Select((Student it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = StudentNote.特殊学生类型.ToString(),
                Value = (spt.HasValue ? ((int)spt.Value).ToString() : null)
            }).ToList();
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchPatch(patchs);
            if (commandResult.Result == ResultCode.Successful)
            {
                foreach (Student student in students)
                {
                    student.SetProperty(StudentNote.特殊学生类型, spt.HasValue ? ((int)spt.Value).ToString() : null);
                }
            }
            return commandResult;
        }

        public QueryResult<StudentPhoto> GetPhoto(Guid StudentID)
        {
            return ProviderFactory.Create<IProvider<StudentPhoto, Guid>>(base.RepoUri).GetByID(StudentID);
        }

        public CommandResult SavePhoto(Guid StudentID, string path, bool overWrite = true)
        {
            StudentPhoto sp = new StudentPhoto(StudentID, path);
            return SavePhoto(sp, overWrite);
        }

        public CommandResult SavePhoto(StudentPhoto sp, bool overWrite = true)
        {
            StudentPhoto queryObject = GetPhoto(sp.ID).QueryObject;
            if (queryObject != null)
            {
                if (!overWrite)
                {
                    return new CommandResult(ResultCode.Successful, "已有照片");
                }
                CommandResult commandResult = DeletePhoto(sp.ID);
                if (commandResult.Result != ResultCode.Successful)
                {
                    return commandResult;
                }
            }
            return AddPhoto(sp);
        }

        public CommandResult AddPhoto(StudentPhoto sp)
        {
            return ProviderFactory.Create<IProvider<StudentPhoto, Guid>>(base.RepoUri).Insert(sp);
        }

        public CommandResult DeletePhoto(Guid studentID)
        {
            StudentPhoto studentPhoto = new StudentPhoto();
            studentPhoto.ID = studentID;
            return ProviderFactory.Create<IProvider<StudentPhoto, Guid>>(base.RepoUri).Delete(studentPhoto);
        }

        public CommandResult SavePhotos(PhysicalProject project, List<StudentPhoto> photoes, ImportOption option)
        {
            return ProviderFactory.Create<IBatchProvider<StudentPhoto, Guid>>(base.RepoUri).BatchAdd(photoes, project.ID, option);
        }

        public QueryResultList<StudentPhoto> GetPhotos(StudentSearchCondition con)
        {
            return ProviderFactory.Create<IProvider<StudentPhoto, Guid>>(base.RepoUri).GetItems(con);
        }
    }
}
