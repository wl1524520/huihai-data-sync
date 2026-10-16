using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
namespace LJH.GeneralLibrary.Core.DAL
{
	public interface IProvider<TInfo, TID> where TInfo : class, IEntity<TID>
	{
		QueryResult<TInfo> GetByID(TID id);

		QueryResultList<TInfo> GetItems(SearchCondition search);

		CommandResult Insert(TInfo t);

		CommandResult Update(TInfo newVal, TInfo originalVal);

		CommandResult Delete(TInfo t);

		IUnitWork CreateUnitWork();

		void Insert(TInfo t, IUnitWork unitWork);

		void Update(TInfo newVal, TInfo originalVal, IUnitWork unitWork);

		void Delete(TInfo t, IUnitWork unitWork);
	}

}
