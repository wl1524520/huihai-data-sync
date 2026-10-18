using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using HH.GeneralLibrary.ExceptionHandling;
namespace HH.GeneralLibrary.Core.DAL.Linq
{
	public class LinqUnitWork : IUnitWork
	{
		private DataContext _DataContext;

		public DataContext DataContext
		{
			get
			{
				return _DataContext;
			}
		}

		public LinqUnitWork(SQLConnectionURI connStr, MappingSource ms)
		{
			_DataContext = DataContextFactory.CreateDataContext(connStr, ms);
		}

		public LinqUnitWork(string connStr, MappingSource ms)
		{
			_DataContext = DataContextFactory.CreateDataContext(connStr, ms);
		}

		public CommandResult Commit()
		{
			try
			{
				_DataContext.SubmitChanges();
				return new CommandResult(ResultCode.Successful, ResultCode.Successful.ToString());
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex, "UnitWork.Commit()");
				return new CommandResult(ResultCode.Fail, ex.Message);
			}
		}
	}

}
