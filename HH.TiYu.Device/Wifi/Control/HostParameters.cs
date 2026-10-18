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


namespace HH.TiYu.Device.Wifi
{
    /// <summary>主机参数，按设备协议原始编码读写；开关和模式保留 byte 值，含义以设备协议为准。</summary>
    public class HostParameters
    {
        /// <summary>按键学号输入：学号最大长度。</summary>
        public byte KeypadStudentIdMaxLength { get; set; }

        /// <summary>按键学号输入：递增间隔。</summary>
        public byte KeypadStudentIdIncrement { get; set; }

        /// <summary>按键学号输入：人工确认。</summary>
        public byte KeypadStudentIdManualConfirmation { get; set; }

        /// <summary>输入设备：扫描枪或身份证。</summary>
        public byte ScannerOrIdentityCardInput { get; set; }

        /// <summary>输入设备：IC卡物理号。</summary>
        public byte IcCardPhysicalNumberInput { get; set; }

        /// <summary>测试结果：确认方式。</summary>
        public byte ResultConfirmationMode { get; set; }

        /// <summary>测试结果：写入IC卡。</summary>
        public byte ResultWriteToIcCard { get; set; }

        /// <summary>测试结果：LED屏上显示。</summary>
        public byte ResultDisplayOnLed { get; set; }

        /// <summary>辅助设备：无线音箱。</summary>
        public byte WirelessSpeaker { get; set; }

        /// <summary>辅助设备：红外防枪跑器。</summary>
        public byte InfraredFalseStartDetector { get; set; }

        /// <summary>测试限制：测试次数。</summary>
        public byte TestAttemptLimit { get; set; }

        /// <summary>测试限制：总时间。</summary>
        public int TestTimeLimit { get; set; }

        /// <summary>测试限制：进行方式。</summary>
        public byte TestExecutionMode { get; set; }

        /// <summary>引体向上：难度系数。</summary>
        public byte PullUpDifficulty { get; set; }

        /// <summary>地毯式中长跑：跑道长度。</summary>
        public int MatRunTrackLength { get; set; }

        /// <summary>地毯式中长跑：男生测试长度。</summary>
        public int MatRunMaleDistance { get; set; }

        /// <summary>地毯式中长跑：女生测试长度。</summary>
        public int MatRunFemaleDistance { get; set; }

        /// <summary>地毯式中长跑：测试模式。</summary>
        public byte MatRunTestMode { get; set; }

        /// <summary>标签式中长跑：圈数设定。</summary>
        public byte TagRunLapCount { get; set; }

        /// <summary>标签式中长跑：测试模式。</summary>
        public byte TagRunTestMode { get; set; }

        /// <summary>仰卧起坐：躺平模式。</summary>
        public byte SitUpLyingMode { get; set; }

        /// <summary>短跑系列：项目选择。</summary>
        public byte SprintEventSelection { get; set; }
    }
}
