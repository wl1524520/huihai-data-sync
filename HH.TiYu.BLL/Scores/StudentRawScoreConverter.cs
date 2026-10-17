using System;
using HH.TiYu.Model;
using HH.Zhongkao.Device;

namespace HH.TiYu.BLL
{
    /// <summary>设备记录到原始成绩的转换；保留现有 Key、时间与特殊成绩规则。</summary>
    internal static class StudentRawScoreConverter
    {
        internal static StudentRawScore Convert(string projectID, PhysicalRecord record, ScoreSource scoreFrom, string hostSN)
        {
            DateTime recordTime = record.RecordDate ?? DateTime.Now;
            StudentRawScore studentRawScore = new StudentRawScore();
            studentRawScore.ID = Guid.NewGuid();
            studentRawScore.ProjectID = projectID;
            studentRawScore.Key = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}", projectID, hostSN, record.SN, record.StudentNumber, record.PhysicalItem, record.Result, recordTime.ToString("yyyyMMddHHmmss"));
            studentRawScore.StudentID = record.StudentNumber;
            studentRawScore.StudentName = record.Name;
            studentRawScore.Sex = record.Sex;
            studentRawScore.TestID = record.PhysicalItem;
            studentRawScore.Score = record.Result;
            studentRawScore.CreateTime = recordTime;
            studentRawScore.HostSn = hostSN;
            studentRawScore.HostID = record.HostID;
            studentRawScore.ScoreSer = record.SN;
            studentRawScore.ScoreFrom = scoreFrom;
            if (record.SpecialResult.HasValue)
            {
                if (record.SpecialResult == 160)
                {
                    studentRawScore.SpecialType = SpecialScoreType.Foul;
                }
                else if (record.SpecialResult == 161)
                {
                    studentRawScore.SpecialType = SpecialScoreType.Abandoned;
                }
                else if (record.SpecialResult == 162)
                {
                    studentRawScore.SpecialType = SpecialScoreType.Incomplete;
                }
                else
                {
                    studentRawScore.SpecialType = SpecialScoreType.Foul;
                }
            }
            return studentRawScore;
        }
    }
}
