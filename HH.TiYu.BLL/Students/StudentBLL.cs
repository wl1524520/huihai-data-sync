// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.BLL
{
    public class StudentBLL : BLLBase<string, Student>
    {
        public StudentBLL(string repoUri)
            : base(repoUri)
        {
        }

    }
}
