using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Data.SqlClient;
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
namespace HH.GeneralLibrary.Core.DAL.Linq
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
