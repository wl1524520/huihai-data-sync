// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using LJH.GeneralLibrary.Core.DAL;

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

        public CommandResult AddStudents(PhysicalProject project, List<Student> students, bool ignore)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<Student, Guid>>(base.RepoUri).BatchAdd(students, project.ID, (!ignore) ? ImportOption.Override : ImportOption.Ignore);
            if (commandResult.Result == ResultCode.Successful)
            {
                new PhysicalProjectBLL(base.RepoUri).GetFullInfo(project, 项目加载项.学生信息);
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
    }
}
