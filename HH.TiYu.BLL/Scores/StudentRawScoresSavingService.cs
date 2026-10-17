using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HH.TiYu.DAL;
using HH.TiYu.DAL.LinqProvider;
using HH.TiYu.Model;
using HH.Zhongkao.Device;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;

namespace HH.TiYu.BLL
{
	public class StudentRawScoresSavingService
	{
        private readonly object _UnsavedRecordsLocker = new object();
        private readonly object _lifecycleLocker = new object();
        private readonly List<StudentRawScore> _UnsavedItems = new List<StudentRawScore>();
        private readonly AutoResetEvent _wake = new AutoResetEvent(false);
        private Thread _SaveRecordsTask;
        private bool _stopping;
        private bool _stopRequested;

        public static StudentRawScoresSavingService Current { get; set; }

        /// <summary>包含正在提交的批次；只有提交成功才从队列移除。</summary>
        public int PendingCount { get { lock (_UnsavedRecordsLocker) return _UnsavedItems.Count; } }

        private void SaveRecordsTask()
        {
            while (true)
            {
                List<StudentRawScore> batch;
                lock (_UnsavedRecordsLocker)
                {
                    if (_stopRequested) return;
                    // 按记录所属项目分批，切换当前项目不会遗留旧项目的待保存成绩。
                    string projectID = _UnsavedItems.Count == 0 ? null : _UnsavedItems[0].ProjectID;
                    batch = _UnsavedItems.Where(it => it.ProjectID == projectID).Take(500).ToList();
                }
                bool saved = false;
                if (batch.Count > 0)
                {
                    try
                    {
                        CommandResult result = ProviderFactory.Create<StudentRawScoreProvider>(AppSettings.Current.StudentRawScoreDB).BatchAdd(batch, batch[0].ProjectID);
                        saved = result != null && result.Result == ResultCode.Successful;
                        if (!saved) FileLog.Log("原始成绩保存", "原始成绩保存到本地失败：" + (result == null ? "无返回结果" : result.Message));
                    }
                    catch (Exception ex) { ExceptionPolicy.HandleException(ex); }
                    if (saved)
                    {
                        lock (_UnsavedRecordsLocker)
                        {
                            foreach (StudentRawScore item in batch) _UnsavedItems.Remove(item);
                            Monitor.PulseAll(_UnsavedRecordsLocker);
                        }
                    }
                }
                if (!saved) _wake.WaitOne(1000);
            }
        }

        public void Start()
        {
            lock (_lifecycleLocker) StartCore();
        }

        private void StartCore()
        {
            lock (_UnsavedRecordsLocker)
            {
                if (_SaveRecordsTask != null && _SaveRecordsTask.IsAlive) return;
                _stopping = false;
                _stopRequested = false;
                _SaveRecordsTask = new Thread(SaveRecordsTask);
                _SaveRecordsTask.IsBackground = true;
                _SaveRecordsTask.Start();
            }
        }

        /// <summary>唤醒保存线程，等待所有已排队及正在提交的记录保存成功。失败保留队列。</summary>
        public bool Flush(TimeSpan timeout)
        {
            lock (_lifecycleLocker) return FlushCore(timeout);
        }

        private bool FlushCore(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue) throw new ArgumentOutOfRangeException("timeout");
            Start();
            _wake.Set();
            Stopwatch watch = Stopwatch.StartNew();
            lock (_UnsavedRecordsLocker)
            {
                while (_UnsavedItems.Count > 0)
                {
                    int remaining = (int)Math.Ceiling(timeout.TotalMilliseconds - watch.Elapsed.TotalMilliseconds);
                    if (remaining <= 0) return false;
                    Monitor.Wait(_UnsavedRecordsLocker, remaining);
                }
                return true;
            }
        }

        /// <summary>停止接收新记录，排空后协作退出；超时返回 false，线程和未保存队列仍保留供重试。</summary>
        public bool Stop(TimeSpan timeout)
        {
            lock (_lifecycleLocker) return StopCore(timeout);
        }

        private bool StopCore(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue) throw new ArgumentOutOfRangeException("timeout");
            Start();
            lock (_UnsavedRecordsLocker) _stopping = true;
            Stopwatch watch = Stopwatch.StartNew();
            if (!Flush(timeout)) return false;
            Thread worker;
            lock (_UnsavedRecordsLocker) { _stopRequested = true; worker = _SaveRecordsTask; }
            _wake.Set();
            return worker.Join(Math.Max(0, (int)(timeout.TotalMilliseconds - watch.Elapsed.TotalMilliseconds)));
        }

        public void Stop()
        {
            if (!Stop(TimeSpan.FromSeconds(10))) throw new TimeoutException("原始成绩未全部保存，待保存数量：" + PendingCount);
        }

		public void AddScore(string projectID, PhysicalRecord record, ScoreSource scoreFrom, string hostSN = null)
		{
			lock (_UnsavedRecordsLocker)
			{
                if (_stopping) throw new InvalidOperationException("保存服务正在停止，不能再添加成绩。");
				_UnsavedItems.Add(StudentRawScoreConverter.Convert(projectID, record, scoreFrom, hostSN));
                _wake.Set();
			}
		}
	}
}
