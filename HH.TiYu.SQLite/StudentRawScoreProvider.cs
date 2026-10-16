using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using HH.TiYu.Model;
using HH.Zhongkao.Device;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;

namespace HH.TiYu.SQLite
{
	public class StudentRawScoreProvider : ProviderBase<StudentRawScore, Guid>
	{
		public StudentRawScoreProvider(string connStr, MappingSource ms)
			: base(connStr, ms)
		{
		}

		protected override StudentRawScore GetingItemByID(Guid id, DataContext dc)
		{
			return dc.GetTable<StudentRawScore>().SingleOrDefault((StudentRawScore item) => item.ID == id);
		}

		protected override List<StudentRawScore> GetingItems(DataContext dc, SearchCondition search)
		{
			IQueryable<StudentRawScore> source = dc.GetTable<StudentRawScore>();
			if (search is StudentRawScoreSearchCondition)
			{
				StudentRawScoreSearchCondition con = search as StudentRawScoreSearchCondition;
				if (con.DateTimeRange != null)
				{
					source = source.Where((StudentRawScore it) => it.CreateTime >= con.DateTimeRange.Begin && it.CreateTime <= con.DateTimeRange.End);
				}
				if (!string.IsNullOrEmpty(con.StudentIDLike))
				{
					source = source.Where((StudentRawScore it) => it.StudentID.Contains(con.StudentIDLike));
				}
				if (!string.IsNullOrEmpty(con.Name))
				{
					source = source.Where((StudentRawScore it) => it.StudentName.Contains(con.Name));
				}
				if (con.TestID.HasValue)
				{
					source = source.Where((StudentRawScore it) => (int?)it.TestID == con.TestID);
				}
				if (!string.IsNullOrEmpty(con.HostSN))
				{
					source = source.Where((StudentRawScore it) => it.HostSn == con.HostSN);
				}
				if (!string.IsNullOrEmpty(con.ProjectID))
				{
					source = source.Where((StudentRawScore it) => it.ProjectID == con.ProjectID);
				}
			}
			List<StudentRawScore> list = null;
			if (search.SortMode == SortMode.Asc)
			{
				return (from sc in source
					orderby sc.HostSn, sc.ScoreSer, sc.CreateTime
					select sc).ToList();
			}
			if (search.SortMode == SortMode.Desc)
			{
				return (from sc in source
					orderby sc.HostSn descending, sc.ScoreSer descending, sc.CreateTime descending
					select sc).ToList();
			}
			return source.ToList();
		}

		public override QueryResultList<StudentRawScore> GetItems(SearchCondition search)
		{
			QueryResultList<StudentRawScore> items = base.GetItems(search);
			items.TotalCount = ((items.QueryObjects != null) ? items.QueryObjects.Count : 0);
			items.PageIndex = ((search == null) ? 1 : search.PageIndex);
			items.PageSize = ((search != null) ? search.PageSize : 0);
			if (search != null && search.PageSize > 0 && search.PageIndex > 0 && items.QueryObjects != null)
			{
				List<StudentRawScore> queryObjects = items.QueryObjects.Skip(search.PageSize * (search.PageIndex - 1)).Take(search.PageSize).ToList();
				items.QueryObjects = queryObjects;
			}
			return items;
		}

		public CommandResult BatchAdd(List<StudentRawScore> scores, string projectID)
		{
			try
			{
				DataContext dataContext = CreateDataContext();
				Dictionary<string, StudentRawScore> dictionary = new Dictionary<string, StudentRawScore>();
				List<string> ids = scores.Select((StudentRawScore it) => it.Key).Distinct().ToList();
				List<StudentRawScore> list = (from it in dataContext.GetTable<StudentRawScore>()
					where it.ProjectID == projectID && ids.Contains(it.Key)
					select it).ToList();
				foreach (StudentRawScore item in list)
				{
					if (!dictionary.ContainsKey(item.Key))
					{
						dictionary.Add(item.Key, item);
					}
				}
				foreach (StudentRawScore score in scores)
				{
					if (!dictionary.ContainsKey(score.Key))
					{
						score.ProjectID = projectID;
						dataContext.GetTable<StudentRawScore>().InsertOnSubmit(score);
						dictionary.Add(score.Key, score);
					}
				}
				dataContext.SubmitChanges();
				return new CommandResult(ResultCode.Successful, string.Empty);
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
				return new CommandResult(ResultCode.Fail, ex.Message);
			}
		}

		public CommandResult BatchDelete(List<StudentRawScore> items, string projectID)
		{
			try
			{
				DataContext dataContext = CreateDataContext();
				foreach (StudentRawScore item in items)
				{
					DeletingItem(item, dataContext);
				}
				dataContext.SubmitChanges();
				return new CommandResult(ResultCode.Successful, string.Empty);
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
				return new CommandResult(ResultCode.Fail, ex.Message);
			}
		}
	}
}
