// Recovered from lib/Model.dll with ILSpy; see README.md.
using System.Collections.Generic;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model.SearchCondition
{
    /// <summary>
    /// 学生查询条件。只保留采集器仍能表达的筛选:项目、
    /// 指定标识集合。原实现里的毕业状态、卡号、写卡开关依赖已取消持久化的字段
    /// 或已不存在的业务(卡号平台不再下发),本轮移除。
    /// </summary>
    public class StudentSearchCondition : HH.GeneralLibrary.Core.DAL.SearchCondition
    {
        public string ProjectID { get; set; }


        public List<string> StudentIDs { get; set; }
    }
}
