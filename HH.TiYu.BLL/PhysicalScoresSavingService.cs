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
    public class PhysicalScoresSavingService
    {
        private object _UnsavedRecordsLocker = new object();

        private Thread _SaveRecordsTask = null;

        private List<PhysicalScore> _UnsavedItems = new List<PhysicalScore>();

        public bool Log { get; set; }

        public PhysicalScoresSavingService()
        {
            if (_SaveRecordsTask == null)
            {
                _SaveRecordsTask = new Thread(SaveRecordsTask);
                _SaveRecordsTask.IsBackground = true;
                _SaveRecordsTask.Start();
            }
        }

        private void SaveRecordsTask()
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            try
            {
                PhysicalProjectBLL physicalProjectBLL = new PhysicalProjectBLL(AppSettings.Current.ConnStr);
                List<PhysicalScore> list = new List<PhysicalScore>();
                while (true)
                {
                    lock (_UnsavedRecordsLocker)
                    {
                        if (_UnsavedItems.Count > 0)
                        {
                            list.AddRange(_UnsavedItems);
                            _UnsavedItems.Clear();
                        }
                    }
                    if (list.Count > 0)
                    {
                        CommandResult commandResult = physicalProjectBLL.AddScores(PhysicalProject.Current, list, ImportOption.Append);
                        num += list.Count;
                        if (commandResult.Result == ResultCode.Successful)
                        {
                            num2 += list.Count;
                        }
                        else
                        {
                            num3 += list.Count;
                            lock (_UnsavedRecordsLocker)
                            {
                                _UnsavedItems.AddRange(list);
                            }
                        }
                        list.Clear();
                        if (Log)
                        {
                            FileLog.Log("测试成绩保存", string.Format("共保存 {0} 条数据 其中成功 {1} 条，失败 {2} 条", num, num2, num3));
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex2)
            {
                ExceptionPolicy.HandleException(ex2);
            }
            _SaveRecordsTask = null;
        }

        public void AddScores(List<PhysicalScore> scores)
        {
            // 采集器精简库不含 PhysicalScore 表；保留原补丁语义，不排队写入正式评分成绩。
            // 原始成绩仍由 HH.TiYu.SQLite.StudentRawScoresSavingService 保存。
        }

        public void AddScore(PhysicalScore score)
        {
            // 采集器精简库不含 PhysicalScore 表；保留原补丁语义，不排队写入正式评分成绩。
            // 原始成绩仍由 HH.TiYu.SQLite.StudentRawScoresSavingService 保存。
        }
    }
}
