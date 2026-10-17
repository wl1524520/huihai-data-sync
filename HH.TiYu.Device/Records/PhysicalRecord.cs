// Recovered from lib/Device.dll; see README.md.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HH.GeneralLibrary;
using HH.GeneralLibrary.ExceptionHandling;
using HH.GeneralLibrary.LOG;
using HH.GeneralLibrary.Net;


namespace HH.Zhongkao.Device
{
    public class PhysicalRecord
    {
        public int SN { get; set; }

        public string StudentNumber { get; set; }

        public string Name { get; set; }

        public string Sex { get; set; }

        public byte PhysicalItem { get; set; }

        public decimal Result { get; set; }

        public int? SpecialResult { get; set; }

        public DateTime? RecordDate { get; set; }

        public int HostID { get; set; }

        public bool Invalid { get; set; }

        public IDSource IDSource { get; set; }

        public byte[] Data { get; set; }

        public override string ToString()
        {
            try
            {
                if (RecordDate.HasValue)
                {
                    return string.Format("主机号:{0} SN:{1} 学号:{2} 姓名:{3} 性别:{4} 测试项:{5} 测试成绩:{6} 时间:{7} {8}", HostID, SN, StudentNumber, Name, Sex, PhysicalItem, Result, RecordDate.Value.ToString("yyyy-MM-dd HH:mm"), Invalid ? "无效记录" : string.Empty);
                }
                return string.Format("SN:{0} 学号:{1} 姓名:{2} 性别:{3} 测试项:{4} 测试成绩:{5} {6}", SN, StudentNumber, Name, Sex, PhysicalItem, Result, Invalid ? "无效记录" : string.Empty);
            }
            catch
            {
            }
            return base.ToString();
        }
    }
}
