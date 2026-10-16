// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;
using LJH.GeneralLibrary.ExceptionHandling;

namespace HH.TiYu.DAL.LinqProvider
{
    public class AutoNumberCreater
    {
        private string ConnectStr;

        private MappingSource _MappingSource = null;

        public AutoNumberCreater(string connStr, MappingSource ms)
        {
            ConnectStr = connStr;
            _MappingSource = ms;
        }

        public string CreateNumber(string prefix, string dateFormat, int serialLen, string type)
        {
            try
            {
                string head = string.Format("{0}-{1}-", prefix, DateTime.Now.ToString(dateFormat));
                DataContext dataContext = DataContextFactory.CreateDataContext(ConnectStr, _MappingSource);
                AutoCreateNumber autoCreateNumber = dataContext.GetTable<AutoCreateNumber>().SingleOrDefault((AutoCreateNumber item) => item.Prefix == head && item.NumberType == type);
                if (autoCreateNumber == null)
                {
                    autoCreateNumber = new AutoCreateNumber
                    {
                        Prefix = head,
                        Number = 1,
                        NumberType = type
                    };
                    dataContext.GetTable<AutoCreateNumber>().InsertOnSubmit(autoCreateNumber);
                }
                else
                {
                    autoCreateNumber.Number++;
                }
                dataContext.SubmitChanges();
                return autoCreateNumber.Prefix + autoCreateNumber.Number.ToString("D" + serialLen);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
            }
            return null;
        }

        public string CreateNumber(string prefix, int serialLen, string type)
        {
            try
            {
                DataContext dataContext = DataContextFactory.CreateDataContext(ConnectStr, _MappingSource);
                AutoCreateNumber autoCreateNumber = dataContext.GetTable<AutoCreateNumber>().SingleOrDefault((AutoCreateNumber item) => item.Prefix == prefix && item.NumberType == type);
                if (autoCreateNumber == null)
                {
                    autoCreateNumber = new AutoCreateNumber
                    {
                        Prefix = prefix,
                        Number = 1,
                        NumberType = type
                    };
                    dataContext.GetTable<AutoCreateNumber>().InsertOnSubmit(autoCreateNumber);
                }
                else
                {
                    autoCreateNumber.Number++;
                }
                dataContext.SubmitChanges();
                return autoCreateNumber.Prefix + autoCreateNumber.Number.ToString("D" + serialLen);
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
            }
            return null;
        }
    }
}
