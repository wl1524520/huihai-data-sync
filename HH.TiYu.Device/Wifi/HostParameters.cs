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
using LJH.GeneralLibrary;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;
using LJH.GeneralLibrary.Net;


namespace HH.Zhongkao.Device.Wifi
{
    public class HostParameters
    {
        public byte 按键学号输入_学号最大长度 { get; set; }

        public byte 按键学号输入_递增间隔 { get; set; }

        public byte 按键学号输入_人工确认 { get; set; }

        public byte 输入设备_扫描枪或身份证 { get; set; }

        public byte 输入设备_IC卡物理号 { get; set; }

        public byte 测试结果_确认方式 { get; set; }

        public byte 测试结果_写入IC卡 { get; set; }

        public byte 测试结果_LED屏上显示 { get; set; }

        public byte 辅助设备_无线音箱 { get; set; }

        public byte 辅助设备_红外防枪跑器 { get; set; }

        public byte 测试限制_测试次数 { get; set; }

        public int 测试限制_总时间 { get; set; }

        public byte 测试限制_进行方式 { get; set; }

        public byte 引体向上_难度系数 { get; set; }

        public int 地毯式中长跑_跑道长度 { get; set; }

        public int 地毯式中长跑_男生测试长度 { get; set; }

        public int 地毯式中长跑_女生测试长度 { get; set; }

        public byte 地毯式中长跑_测试模式 { get; set; }

        public byte 标签式中长跑_圈数设定 { get; set; }

        public byte 标签式中长跑_测试模式 { get; set; }

        public byte 仰卧起坐_躺平模式 { get; set; }

        public byte 短跑系列_项目选择 { get; set; }
    }
}
