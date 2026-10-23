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

		// ==================== 成绩上传原语 ====================
		// Uploaded 列刻意不进实体映射(见 DAL README),以下方法经 ExecuteQuery/ExecuteCommand
		// 直接读写该列。上传业务(白名单来源、分批、ACK 后置位)留在采集器 ScoreUpload;
		// 上传口径与 SQL 在此单点维护,取数/置位/重置/统计不会口径漂移。

		/// <summary>待上传成绩行。Mono 为数据库侧计算的 HEX(ID),与 GUID 存储格式(二进制/文本)无关。</summary>
		public class PendingUploadScore
		{
			public string Mono { get; set; }

			public string StudentID { get; set; }

			public string StudentName { get; set; }

			public int TestID { get; set; }

			public decimal? Score { get; set; }

			public DateTime CreateTime { get; set; }
		}

		/// <summary>上传口径:白名单科目 + 非特殊成绩 + 有成绩值。参数占位序号接续 args 现有元素。</summary>
		private static string UploadableFilter(IList<int> uploadableTestIds, List<object> args)
		{
			if (uploadableTestIds == null || uploadableTestIds.Count == 0)
				throw new ArgumentException("可上传科目白名单不能为空");
			var placeholders = new string[uploadableTestIds.Count];
			for (int i = 0; i < uploadableTestIds.Count; i++)
			{
				args.Add(uploadableTestIds[i]);
				placeholders[i] = "{" + (args.Count - 1) + "}";
			}
			return "SpecialType IS NULL AND Score IS NOT NULL AND TestID IN (" + string.Join(",", placeholders) + ")";
		}

		/// <summary>当前项目未上传的可上传成绩,按设备记录时间升序;Uploaded=0 在库内过滤,不物化整表。</summary>
		public List<PendingUploadScore> GetPendingForUpload(string projectID, int[] uploadableTestIds)
		{
			var args = new List<object> { projectID };
			string sql = "SELECT HEX(ID) AS Mono, StudentID, StudentName, TestID, Score, CreateTime FROM StudentScore " +
				"WHERE ProjectID = {0} AND Uploaded = 0 AND " + UploadableFilter(uploadableTestIds, args) +
				" ORDER BY CreateTime";
			using (var dc = CreateDataContext())
			{
				return dc.ExecuteQuery<PendingUploadScore>(sql, args.ToArray()).ToList();
			}
		}

		/// <summary>在单个事务内把给定 HEX(ID) 的成绩标记为已上传;返回实际更新行数。</summary>
		public int MarkUploaded(IEnumerable<string> monos)
		{
			if (monos == null) return 0;
			var list = monos.Distinct().ToList();
			if (list.Count == 0) return 0;
			int updated = 0;
			using (var dc = CreateDataContext())
			{
				dc.Connection.Open();
				using (var transaction = dc.Connection.BeginTransaction())
				{
					dc.Transaction = transaction;
					foreach (string mono in list)
						updated += dc.ExecuteCommand("UPDATE StudentScore SET Uploaded = 1 WHERE HEX(ID) = {0}", mono);
					transaction.Commit();
				}
			}
			return updated;
		}

		/// <summary>重置当前项目全部可上传成绩的上传标记(用于补传);返回影响行数。</summary>
		public int ResetUploaded(string projectID, int[] uploadableTestIds)
		{
			var args = new List<object> { projectID };
			string sql = "UPDATE StudentScore SET Uploaded = 0 WHERE ProjectID = {0} AND " + UploadableFilter(uploadableTestIds, args);
			using (var dc = CreateDataContext())
			{
				return dc.ExecuteCommand(sql, args.ToArray());
			}
		}

		/// <summary>按上传口径统计当前项目已上传(uploaded=1)/未上传(uploaded=0)条数。</summary>
		public int CountByUploaded(string projectID, int uploaded, int[] uploadableTestIds)
		{
			var args = new List<object> { projectID, uploaded };
			string sql = "SELECT COUNT(*) FROM StudentScore WHERE ProjectID = {0} AND Uploaded = {1} AND " + UploadableFilter(uploadableTestIds, args);
			using (var dc = CreateDataContext())
			{
				return dc.ExecuteQuery<int>(sql, args.ToArray()).Single();
			}
		}
	}
}
