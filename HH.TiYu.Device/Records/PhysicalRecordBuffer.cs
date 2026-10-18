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


namespace HH.TiYu.Device
{
    public class PhysicalRecordBuffer
    {
        private List<PhysicalRecord> _Records = new List<PhysicalRecord>();

        private object _RecordsLocker = new object();

        public void Add(PhysicalRecord record)
        {
            lock (_RecordsLocker)
            {
                _Records.Add(record);
            }
        }

        public void AddRange(IEnumerable<PhysicalRecord> records)
        {
            lock (_RecordsLocker)
            {
                foreach (PhysicalRecord record in records)
                {
                    _Records.Add(record);
                }
            }
        }

        public List<PhysicalRecord> Read()
        {
            lock (_RecordsLocker)
            {
                if (_Records.Count > 0)
                {
                    List<PhysicalRecord> list = new List<PhysicalRecord>();
                    list.AddRange(_Records);
                    _Records.Clear();
                    return list;
                }
                return null;
            }
        }
    }
}
