using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.GeneralLibrary.Core.DAL;
using HH.GeneralLibrary.Core.DAL.Linq;
using HH.GeneralLibrary.ExceptionHandling;

namespace HH.TiYu.DAL.LinqProvider
{
	public class StudentScoreProvider : ProviderBase<StudentScore, Guid>
	{
		public StudentScoreProvider(string connStr, MappingSource ms)
			: base(connStr, ms)
		{
		}

		protected override StudentScore GetingItemByID(Guid id, DataContext dc)
		{
			return dc.GetTable<StudentScore>().SingleOrDefault((StudentScore item) => item.ID == id);
		}

		protected override List<StudentScore> GetingItems(DataContext dc, SearchCondition search)
		{
			IQueryable<StudentScore> source = dc.GetTable<StudentScore>();
			if (search is StudentScoreSearchCondition)
			{
				StudentScoreSearchCondition con = search as StudentScoreSearchCondition;
				if (con.DateTimeRange != null)
				{
					source = source.Where((StudentScore it) => it.CreateTime >= con.DateTimeRange.Begin && it.CreateTime <= con.DateTimeRange.End);
				}
				if (!string.IsNullOrEmpty(con.StudentIDLike))
				{
					source = source.Where((StudentScore it) => it.StudentID.Contains(con.StudentIDLike));
				}
				if (!string.IsNullOrEmpty(con.Name))
				{
					source = source.Where((StudentScore it) => it.StudentName.Contains(con.Name));
				}
				if (con.TestID.HasValue)
				{
					source = source.Where((StudentScore it) => (int?)it.TestID == con.TestID);
				}
				if (!string.IsNullOrEmpty(con.HostSN))
				{
					source = source.Where((StudentScore it) => it.HostSn == con.HostSN);
				}
				if (!string.IsNullOrEmpty(con.ProjectID))
				{
					source = source.Where((StudentScore it) => it.ProjectID == con.ProjectID);
				}
			}
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

		public override QueryResultList<StudentScore> GetItems(SearchCondition search)
		{
			QueryResultList<StudentScore> items = base.GetItems(search);
			items.TotalCount = ((items.QueryObjects != null) ? items.QueryObjects.Count : 0);
			items.PageIndex = ((search == null) ? 1 : search.PageIndex);
			items.PageSize = ((search != null) ? search.PageSize : 0);
			if (search != null && search.PageSize > 0 && search.PageIndex > 0 && items.QueryObjects != null)
			{
				List<StudentScore> queryObjects = items.QueryObjects.Skip(search.PageSize * (search.PageIndex - 1)).Take(search.PageSize).ToList();
				items.QueryObjects = queryObjects;
			}
			return items;
		}

		public CommandResult BatchAdd(List<StudentScore> scores, string projectID)
		{
			try
			{
				DataContext dataContext = CreateDataContext();
				Dictionary<string, StudentScore> dictionary = new Dictionary<string, StudentScore>();
				List<string> ids = scores.Select((StudentScore it) => it.Key).Distinct().ToList();
				List<StudentScore> list = (from it in dataContext.GetTable<StudentScore>()
					where it.ProjectID == projectID && ids.Contains(it.Key)
					select it).ToList();
				foreach (StudentScore item in list)
				{
					if (!dictionary.ContainsKey(item.Key))
					{
						dictionary.Add(item.Key, item);
					}
				}
				foreach (StudentScore score in scores)
				{
					if (!dictionary.ContainsKey(score.Key))
					{
						score.ProjectID = projectID;
						dataContext.GetTable<StudentScore>().InsertOnSubmit(score);
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

		public CommandResult BatchDelete(List<StudentScore> items, string projectID)
		{
			try
			{
				DataContext dataContext = CreateDataContext();
				foreach (StudentScore item in items)
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
