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
	public class DataContextFactory
	{
		public static DataContext CreateDataContext(SQLConnectionURI sqlURI, MappingSource ms, bool Log = false)
		{
			Debug.Assert(sqlURI != null, "没有找到有效的数据库连接!");
			Debug.Assert(!string.IsNullOrEmpty(sqlURI.ConnectString), "没有找到有效的数据库连接!");
			IDbConnection dbConnection = null;
			dbConnection = ((!string.IsNullOrEmpty(sqlURI.SQLType) && sqlURI.SQLType.ToUpper() == "SQLITE") ? ((IDbConnection)new SQLiteConnection(sqlURI.ConnectString)) : ((IDbConnection)((string.IsNullOrEmpty(sqlURI.SQLType) || !(sqlURI.SQLType.ToUpper() == "MSSQL")) ? new SqlConnection(sqlURI.ConnectString) : new SqlConnection(sqlURI.ConnectString))));
			DataContext dataContext = new DataContext(dbConnection, ms);
			if (Log)
			{
				dataContext.Log = Console.Out;
			}
			return dataContext;
		}

		public static DataContext CreateDataContext(string sqlUri, MappingSource ms, bool Log = false)
		{
			SQLConnectionURI sqlURI = (string.IsNullOrEmpty(sqlUri) ? null : new SQLConnectionURI(sqlUri));
			return CreateDataContext(sqlURI, ms, Log);
		}
	}

}
