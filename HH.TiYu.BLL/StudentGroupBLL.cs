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
    public class StudentGroupBLL : BLLBase<Guid, StudentGroup>
    {
        public StudentGroupBLL(string repoUri)
            : base(repoUri)
        {
        }

        public CommandResult Check(StudentGroupPair pair, StudentGroup gp, string result, string opt)
        {
            StudentGroupPair studentGroupPair = pair.Clone();
            studentGroupPair.State = 1;
            studentGroupPair.CheckTime = DateTime.Now;
            CommandResult commandResult = ProviderFactory.Create<IProvider<StudentGroupPair, Guid>>(base.RepoUri).Update(studentGroupPair, pair);
            if (commandResult.Result == ResultCode.Successful)
            {
                pair.State = studentGroupPair.State;
                pair.CheckTime = studentGroupPair.CheckTime;
                Student studentByID = PhysicalProject.Current.GetStudentByID(pair.StudentID);
                StudentCheckRecord info = new StudentCheckRecord
                {
                    ID = Guid.NewGuid(),
                    CheckTime = DateTime.Now,
                    CheckResult = result,
                    ProjectID = PhysicalProject.Current.ID,
                    StudentID = pair.StudentID,
                    Name = studentByID.Name,
                    Sex = ((studentByID.Sex == Sex.Male) ? "男" : "女"),
                    Facility = studentByID.FacilityName,
                    GroupID = gp.FullName,
                    Channel = pair.Channel,
                    Operator = opt
                };
                new StudentCheckRecordBLL(base.RepoUri).Add(info);
            }
            return commandResult;
        }

        public CommandResult UndoCheck(StudentGroupPair pair)
        {
            StudentGroupPair studentGroupPair = pair.Clone();
            studentGroupPair.State = 0;
            studentGroupPair.CheckTime = null;
            studentGroupPair.Memo = null;
            CommandResult commandResult = null;
            commandResult = ProviderFactory.Create<IProvider<StudentGroupPair, Guid>>(base.RepoUri).Update(studentGroupPair, pair);
            if (commandResult.Result == ResultCode.Successful)
            {
                pair.State = studentGroupPair.State;
                pair.CheckTime = studentGroupPair.CheckTime;
                pair.Memo = studentGroupPair.Memo;
            }
            return commandResult;
        }

        public CommandResult Absent(StudentGroupPair pair, StudentGroup gp, string opt)
        {
            StudentGroupPair studentGroupPair = pair.Clone();
            studentGroupPair.State = 3;
            studentGroupPair.CheckTime = DateTime.Now;
            studentGroupPair.Memo = "缺考";
            CommandResult commandResult = ProviderFactory.Create<IProvider<StudentGroupPair, Guid>>(base.RepoUri).Update(studentGroupPair, pair);
            if (commandResult.Result == ResultCode.Successful)
            {
                pair.State = studentGroupPair.State;
                pair.CheckTime = studentGroupPair.CheckTime;
                pair.Memo = studentGroupPair.Memo;
                Student studentByID = PhysicalProject.Current.GetStudentByID(pair.StudentID);
                StudentCheckRecord info = new StudentCheckRecord
                {
                    ID = Guid.NewGuid(),
                    CheckTime = DateTime.Now,
                    CheckResult = "缺考",
                    ProjectID = PhysicalProject.Current.ID,
                    StudentID = pair.StudentID,
                    Name = studentByID.Name,
                    Sex = ((studentByID.Sex == Sex.Male) ? "男" : "女"),
                    Facility = studentByID.FacilityName,
                    GroupID = gp.FullName,
                    Channel = pair.Channel,
                    Operator = opt
                };
                new StudentCheckRecordBLL(base.RepoUri).Add(info);
            }
            return commandResult;
        }

        public CommandResult Import(List<StudentGroup> items, string projectID)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<StudentGroup, Guid>>(base.RepoUri).BatchAdd(items, projectID, ImportOption.Override);
            if (commandResult.Result == ResultCode.Successful)
            {
                PhysicalProject.Current.AddGroups(items);
            }
            return commandResult;
        }

        public CommandResult Delete(List<StudentGroup> items)
        {
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<StudentGroup, Guid>>(base.RepoUri).BatchDelete(items, PhysicalProject.Current.ID);
            if (commandResult.Result == ResultCode.Successful)
            {
                PhysicalProject.Current.DeleteGroups(items);
            }
            return commandResult;
        }

        public CommandResult 更新考试时间(List<StudentGroup> groups, string 考试时间)
        {
            List<UpdateItem<Guid>> patchs = groups.Select((StudentGroup it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = "PlanDate",
                Value = 考试时间
            }).ToList();
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<StudentGroup, Guid>>(base.RepoUri).BatchPatch(patchs);
            if (commandResult.Result == ResultCode.Successful)
            {
                foreach (StudentGroup group in groups)
                {
                    group.PlanDate = 考试时间;
                }
            }
            return commandResult;
        }
    }
}
