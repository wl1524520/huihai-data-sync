// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    [Flags]
    public enum ProjectLoadOptions
    {
        /// <summary>加载学校信息。</summary>
        Facilities = 2,
        /// <summary>加载学生信息。</summary>
        Students = 4,
        /// <summary>加载学生成绩（采集器已移除此功能，传入时会明确报不支持）。</summary>
        StudentScores = 8,
        /// <summary>加载测试科目。</summary>
        PhysicalItems = 0x10,
        /// <summary>加载评分标准（采集器已移除此功能，传入时会明确报不支持）。</summary>
        ScoringStandards = 0x20,
        /// <summary>加载综合评价（采集器已移除此功能，传入时会明确报不支持）。</summary>
        OverallEvaluations = 0x40,
        /// <summary>加载运动建议（采集器已移除此功能，传入时会明确报不支持）。</summary>
        ExerciseSuggestions = 0x80,
        /// <summary>加载学生分组（采集器已移除此功能，传入时会明确报不支持）。</summary>
        StudentGroups = 0x100,
        /// <summary>加载运动会分组（采集器已移除此功能，传入时会明确报不支持）。</summary>
        SportsMeetGroups = 0x200
    }
}
