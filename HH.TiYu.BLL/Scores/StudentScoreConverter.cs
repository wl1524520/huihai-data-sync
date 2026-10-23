using System;
using HH.TiYu.Model;
using HH.TiYu.Device;

namespace HH.TiYu.BLL
{
    /// <summary>设备记录到原始成绩的转换；保留现有 Key、时间与特殊成绩规则。</summary>
    internal static class StudentScoreConverter
    {
        internal static StudentScore Convert(string projectID, PhysicalRecord record, ScoreSource scoreFrom, string hostSN)
        {
            DateTime recordTime = record.RecordDate ?? DateTime.Now;
            StudentScore studentScore = new StudentScore();
            studentScore.ID = Guid.NewGuid().ToString();
            studentScore.ProjectID = projectID;
            studentScore.Key = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}", projectID, hostSN, record.SN, record.StudentNumber, record.PhysicalItem, record.Result, recordTime.ToString("yyyyMMddHHmmss"));
            studentScore.StudentID = record.StudentNumber;
            studentScore.IDSource = (int)record.IDSource;
            studentScore.StudentName = record.Name;
            studentScore.Sex = record.Sex;
            studentScore.TestID = record.PhysicalItem;
            studentScore.Score = record.Result;
            studentScore.CreateTime = recordTime;
            studentScore.HostSn = hostSN;
            studentScore.HostID = record.HostID;
            studentScore.ScoreSer = record.SN;
            studentScore.ScoreFrom = scoreFrom;
            if (record.SpecialResult.HasValue)
            {
                if (record.SpecialResult == 160)
                {
                    studentScore.SpecialType = SpecialScoreType.Foul;
                }
                else if (record.SpecialResult == 161)
                {
                    studentScore.SpecialType = SpecialScoreType.Abandoned;
                }
                else if (record.SpecialResult == 162)
                {
                    studentScore.SpecialType = SpecialScoreType.Incomplete;
                }
                else
                {
                    studentScore.SpecialType = SpecialScoreType.Foul;
                }
            }
            return studentScore;
        }
    }
}
