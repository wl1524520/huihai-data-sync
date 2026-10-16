using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;
using LJH.GeneralLibrary.Net;
using LJH.GeneralLibrary.SoftDog;
using LJH.GeneralLibrary.WinformControl;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
namespace LJH.GeneralLibrary.Core.DAL.Linq
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
