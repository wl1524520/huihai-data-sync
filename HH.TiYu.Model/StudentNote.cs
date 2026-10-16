// Recovered from lib/Model.dll with ILSpy; see README.md.
namespace HH.TiYu.Model
{
    /// <summary>
    /// 学生扩展信息键（序列化后存于 Student.Note 的 JSON）。
    /// 原 Model.dll 还包含 毕业学校/身体状况/免测项目/考试时间/考场/备注/学校代码/
    /// 中国美院照片上传时间/院系/专业/特殊学生类型/平时成绩，本轮精简中移除：
    /// 采集器只按考试科目筛选可测项目，其余扩展位在本目录内无读取方。
    /// 保留 考试科目 = 1 的取值与名称，JSON 键与原实现一致。
    /// </summary>
    public enum StudentNote
    {
        考试科目 = 1
    }
}
