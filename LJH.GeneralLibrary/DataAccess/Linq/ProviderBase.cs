using LJH.GeneralLibrary.ExceptionHandling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
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
using LJH.GeneralLibrary.LOG;
namespace LJH.GeneralLibrary.Core.DAL.Linq
{
	public abstract class ProviderBase<TInfo, TID> : IProvider<TInfo, TID> where TInfo : class, IEntity<TID>, new()
	{
		protected readonly string successMsg = "ok";

		protected MappingSource _MappingResource = null;

		public string SqlURI { get; set; }

		public ProviderBase(string conStr, MappingSource ms)
		{
			SqlURI = conStr;
			_MappingResource = ms;
		}

		public virtual DataContext CreateDataContext()
		{
			return DataContextFactory.CreateDataContext(SqlURI, _MappingResource, DALConfig.Default.Log);
		}

		public virtual IUnitWork CreateUnitWork()
		{
			return new LinqUnitWork(new SQLConnectionURI(SqlURI), _MappingResource);
		}

		public virtual QueryResult<TInfo> GetByID(TID id)
		{
			QueryResult<TInfo> result;
			try
			{
				DataContext dc = CreateDataContext();
				TInfo val = GetingItemByID(id, dc);
				result = ((val == null) ? new QueryResult<TInfo>(ResultCode.Fail, string.Format("没有找到ID={0}的数据!", id.ToString()), val) : new QueryResult<TInfo>(ResultCode.Successful, successMsg, val));
				if (DALConfig.Default.Log)
				{
					FileLog.Log("Linq", string.Format("{0}", GetType().Name + ".GetByID()"));
				}
			}
			catch (Exception ex)
			{
				result = new QueryResult<TInfo>(ResultCode.Fail, ex.Message, null);
				ExceptionPolicy.HandleException(ex, GetType().Name + ".GetByID()");
			}
			return result;
		}

		public virtual QueryResultList<TInfo> GetItems(SearchCondition search)
		{
			QueryResultList<TInfo> result;
			try
			{
				DataContext dc = CreateDataContext();
				List<TInfo> list = GetingItems(dc, search);
				result = new QueryResultList<TInfo>(ResultCode.Successful, successMsg, list);
				if (DALConfig.Default.Log)
				{
					FileLog.Log("Linq", string.Format("{0} 返回数据:{1}行", GetType().Name + ".GetItems()", (list != null) ? list.Count : 0));
				}
			}
			catch (Exception ex)
			{
				result = new QueryResultList<TInfo>(ResultCode.Fail, ex.Message, new List<TInfo>());
				ExceptionPolicy.HandleException(ex, GetType().Name + ".GetItems()");
			}
			return result;
		}

		public virtual CommandResult Insert(TInfo info)
		{
			CommandResult result;
			try
			{
				DataContext dataContext = CreateDataContext();
				InsertingItem(info, dataContext);
				dataContext.SubmitChanges();
				result = new CommandResult(ResultCode.Successful, successMsg);
			}
			catch (Exception ex)
			{
				result = new CommandResult(ResultCode.Fail, ex.Message);
				ExceptionPolicy.HandleException(ex, GetType().Name + ".Insert()");
			}
			return result;
		}

		public virtual CommandResult Update(TInfo newVal, TInfo original)
		{
			CommandResult result;
			try
			{
				DataContext dataContext = CreateDataContext();
				UpdatingItem(newVal, original, dataContext);
				dataContext.SubmitChanges();
				result = new CommandResult(ResultCode.Successful, successMsg);
			}
			catch (Exception ex)
			{
				result = new CommandResult(ResultCode.Fail, ex.Message);
				ExceptionPolicy.HandleException(ex, GetType().FullName + ".Update()");
			}
			return result;
		}

		public virtual CommandResult Delete(TInfo info)
		{
			CommandResult result;
			try
			{
				DataContext dataContext = CreateDataContext();
				DeletingItem(info, dataContext);
				dataContext.SubmitChanges();
				result = new CommandResult(ResultCode.Successful, successMsg);
			}
			catch (Exception ex)
			{
				result = new CommandResult(ResultCode.Fail, ex.Message);
				ExceptionPolicy.HandleException(ex, GetType().FullName + ".Delete()");
			}
			return result;
		}

		public void Insert(TInfo info, IUnitWork unitWork)
		{
			if (unitWork != null && unitWork is LinqUnitWork)
			{
				DataContext dataContext = (unitWork as LinqUnitWork).DataContext;
				InsertingItem(info, dataContext);
				return;
			}
			throw new NullReferenceException("参数unitWork为空!");
		}

		public void Update(TInfo newVal, TInfo originalVal, IUnitWork unitWork)
		{
			if (unitWork != null && unitWork is LinqUnitWork)
			{
				DataContext dataContext = (unitWork as LinqUnitWork).DataContext;
				UpdatingItem(newVal, originalVal, dataContext);
				return;
			}
			throw new NullReferenceException("参数unitWork为空!");
		}

		public void Delete(TInfo info, IUnitWork unitWork)
		{
			if (unitWork != null && unitWork is LinqUnitWork)
			{
				DataContext dataContext = (unitWork as LinqUnitWork).DataContext;
				DeletingItem(info, dataContext);
				return;
			}
			throw new NullReferenceException("参数unitWork为空!");
		}

		protected abstract TInfo GetingItemByID(TID id, DataContext dc);

		protected virtual List<TInfo> GetingItems(DataContext dc, SearchCondition search)
		{
			return dc.GetTable<TInfo>().ToList();
		}

		protected virtual void InsertingItem(TInfo info, DataContext dc)
		{
			dc.GetTable<TInfo>().InsertOnSubmit(info);
		}

		protected virtual void UpdatingItem(TInfo newVal, TInfo original, DataContext dc)
		{
			dc.GetTable<TInfo>().Attach(newVal, original);
		}

		protected virtual void DeletingItem(TInfo info, DataContext dc)
		{
			dc.GetTable<TInfo>().Attach(info);
			dc.GetTable<TInfo>().DeleteOnSubmit(info);
		}
	}

}
