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
    public class PhysicalScoreProvider : ProviderBase<PhysicalScore, Guid>, IBatchProvider<PhysicalScore, Guid>, IProvider<PhysicalScore, Guid>
    {
        public PhysicalScoreProvider(string connStr, MappingSource ms)
            : base(connStr, ms)
        {
        }

        protected override PhysicalScore GetingItemByID(Guid id, DataContext dc)
        {
            return dc.GetTable<PhysicalScore>().SingleOrDefault((PhysicalScore item) => item.ID == id);
        }

        protected override List<PhysicalScore> GetingItems(DataContext dc, SearchCondition search)
        {
            SQLConnectionURI sQLConnectionURI = new SQLConnectionURI(base.SqlURI);
            if (!string.IsNullOrEmpty(sQLConnectionURI.SQLType) && sQLConnectionURI.SQLType.ToUpper() == "SQLITE")
            {
                return GetItemsSqlite(search);
            }
            IQueryable<PhysicalScore> source = dc.GetTable<PhysicalScore>();
            if (search is PhysicalScoreSearchCondition)
            {
                PhysicalScoreSearchCondition con = search as PhysicalScoreSearchCondition;
                if (!string.IsNullOrEmpty(con.StudentID))
                {
                    source = source.Where((PhysicalScore item) => item.StudentID == con.StudentID);
                }
                if (!string.IsNullOrEmpty(con.ProjectID))
                {
                    source = source.Where((PhysicalScore item) => item.ProjectID == con.ProjectID);
                }
                if (con.PhysicalItem.HasValue)
                {
                    source = source.Where((PhysicalScore item) => item.PhysicalItem == con.PhysicalItem.Value);
                }
                if (con.Grade.HasValue)
                {
                    source = source.Where((PhysicalScore item) => item.Grade == (int?)con.Grade.Value);
                }
                if (con.DateTimeRange != null)
                {
                    source = source.Where((PhysicalScore item) => item.UpdateDate >= con.DateTimeRange.Begin && item.UpdateDate <= con.DateTimeRange.End);
                }
            }
            return source.ToList();
        }

        private List<PhysicalScore> GetItemsSqlite(SearchCondition search)
        {
            List<PhysicalScore> list = new List<PhysicalScore>();
            using (SQLiteConnection sQLiteConnection = new SQLiteConnection(new SQLConnectionURI(base.SqlURI).ConnectString))
            {
                using (SQLiteCommand sQLiteCommand = new SQLiteCommand(sQLiteConnection))
                {
                    sQLiteConnection.Open();
                    string commandText = "SELECT [t0].[ID], [t0].[StudentID], [t0].[Grade], [t0].[ProjectID], [t0].[PhysicalItem], [t0].[Score],[t0].[Result], [t0].[Jiafen], [t0].[Rank], [t0].[SpecialType], [t0].[Modifier], [t0].[UpdateDate], [t0].[Note] FROM [PhysicalScore] AS [t0] where 1=1 ";
                    sQLiteCommand.CommandText = commandText;
                    if (search is PhysicalScoreSearchCondition)
                    {
                        PhysicalScoreSearchCondition physicalScoreSearchCondition = search as PhysicalScoreSearchCondition;
                        if (!string.IsNullOrEmpty(physicalScoreSearchCondition.StudentID))
                        {
                            sQLiteCommand.CommandText += " and StudentID=@p0 ";
                            sQLiteCommand.Parameters.AddWithValue("@p0", physicalScoreSearchCondition.StudentID);
                        }
                        if (!string.IsNullOrEmpty(physicalScoreSearchCondition.ProjectID))
                        {
                            sQLiteCommand.CommandText += " and ProjectID=@p1 ";
                            sQLiteCommand.Parameters.AddWithValue("@p1", physicalScoreSearchCondition.ProjectID);
                        }
                        if (physicalScoreSearchCondition.PhysicalItem.HasValue)
                        {
                            sQLiteCommand.CommandText += " and PhysicalItem=@p2 ";
                            sQLiteCommand.Parameters.AddWithValue("@p2", physicalScoreSearchCondition.PhysicalItem.Value);
                        }
                        if (physicalScoreSearchCondition.Grade.HasValue)
                        {
                            sQLiteCommand.CommandText += " and Grade=@p3 ";
                            sQLiteCommand.Parameters.AddWithValue("@p3", physicalScoreSearchCondition.Grade.Value);
                        }
                        if (physicalScoreSearchCondition.DateTimeRange != null)
                        {
                            sQLiteCommand.CommandText += " and UpdateDate>=@p4 and UpdateDate<=@p5 ";
                            sQLiteCommand.Parameters.AddWithValue("@p4", physicalScoreSearchCondition.DateTimeRange.Begin);
                            sQLiteCommand.Parameters.AddWithValue("@p5", physicalScoreSearchCondition.DateTimeRange.End);
                        }
                    }
                    using (SQLiteDataReader sQLiteDataReader = sQLiteCommand.ExecuteReader())
                    {
                        while (sQLiteDataReader.Read())
                        {
                            list.Add(GetData(sQLiteDataReader));
                        }
                    }
                }
            }
            return list;
        }

        private PhysicalScore GetData(SQLiteDataReader reader)
        {
            PhysicalScore physicalScore = new PhysicalScore();
            physicalScore.ID = reader.GetGuid(0);
            physicalScore.StudentID = reader.GetString(1);
            if (!reader.IsDBNull(2))
            {
                physicalScore.Grade = reader.GetInt32(2);
            }
            if (!reader.IsDBNull(3))
            {
                physicalScore.ProjectID = reader.GetString(3);
            }
            if (!reader.IsDBNull(4))
            {
                physicalScore.PhysicalItem = reader.GetInt32(4);
            }
            if (!reader.IsDBNull(5))
            {
                physicalScore.Score = reader.GetDecimal(5);
            }
            if (!reader.IsDBNull(6))
            {
                physicalScore.Result = reader.GetDecimal(6);
            }
            if (!reader.IsDBNull(7))
            {
                physicalScore.Jiafen = reader.GetDecimal(7);
            }
            if (!reader.IsDBNull(8))
            {
                physicalScore.Rank = reader.GetString(8);
            }
            if (!reader.IsDBNull(9))
            {
                physicalScore.SpecialType = (SpecialScoreType)reader.GetInt32(9);
            }
            if (!reader.IsDBNull(10))
            {
                physicalScore.Modifier = reader.GetString(10);
            }
            if (!reader.IsDBNull(11))
            {
                physicalScore.UpdateDate = reader.GetDateTime(11);
            }
            if (!reader.IsDBNull(12))
            {
                physicalScore.Note = reader.GetString(12);
            }
            return physicalScore;
        }

        public CommandResult BatchAdd(List<PhysicalScore> scores, string projectID, ImportOption option)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                Dictionary<string, List<PhysicalScore>> dictionary = new Dictionary<string, List<PhysicalScore>>();
                if (option != ImportOption.Append)
                {
                    List<string> ids = scores.Select((PhysicalScore it) => it.StudentID).Distinct().ToList();
                    List<PhysicalScore> list = (from it in dataContext.GetTable<PhysicalScore>()
                        where it.ProjectID == projectID && ids.Contains(it.StudentID)
                        select it).ToList();
                    foreach (PhysicalScore item in list)
                    {
                        if (!dictionary.ContainsKey(item.StudentID))
                        {
                            dictionary.Add(item.StudentID, new List<PhysicalScore>());
                        }
                        dictionary[item.StudentID].Add(item);
                    }
                }
                foreach (PhysicalScore score in scores)
                {
                    switch (option)
                    {
                    case ImportOption.Append:
                        dataContext.GetTable<PhysicalScore>().InsertOnSubmit(score);
                        break;
                    case ImportOption.Ignore:
                        if (!dictionary.ContainsKey(score.StudentID) || !dictionary[score.StudentID].Exists((PhysicalScore it) => it.PhysicalItem == score.PhysicalItem))
                        {
                            dataContext.GetTable<PhysicalScore>().InsertOnSubmit(score);
                        }
                        break;
                    case ImportOption.Override:
                        if (dictionary.ContainsKey(score.StudentID) && dictionary[score.StudentID].Exists((PhysicalScore it) => it.PhysicalItem == score.PhysicalItem))
                        {
                            dataContext.GetTable<PhysicalScore>().DeleteAllOnSubmit(dictionary[score.StudentID].Where((PhysicalScore it) => it.PhysicalItem == score.PhysicalItem));
                            dictionary[score.StudentID].RemoveAll((PhysicalScore it) => it.PhysicalItem == score.PhysicalItem);
                        }
                        dataContext.GetTable<PhysicalScore>().InsertOnSubmit(score);
                        break;
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

        public CommandResult BatchDelete(List<PhysicalScore> items, string projectID)
        {
            try
            {
                DataContext dataContext = CreateDataContext();
                foreach (PhysicalScore item in items)
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

        public CommandResult BatchPatch(List<UpdateItem<Guid>> patchs)
        {
            try
            {
                List<Guid> ids = patchs.Select((UpdateItem<Guid> it) => it.ID).Distinct().ToList();
                DataContext dataContext = CreateDataContext();
                List<PhysicalScore> list = (from it in dataContext.GetTable<PhysicalScore>()
                    where ids.Contains(it.ID)
                    select it).ToList();
                Dictionary<Guid, PhysicalScore> dictionary = new Dictionary<Guid, PhysicalScore>();
                foreach (PhysicalScore item in list)
                {
                    if (!dictionary.ContainsKey(item.ID))
                    {
                        dictionary.Add(item.ID, item);
                    }
                }
                foreach (UpdateItem<Guid> patch in patchs)
                {
                    if (!dictionary.ContainsKey(patch.ID))
                    {
                        continue;
                    }
                    PhysicalScore physicalScore = dictionary[patch.ID];
                    switch (patch.Key)
                    {
                    case "Score":
                        physicalScore.Score = decimal.Parse(patch.Value);
                        break;
                    case "Result":
                        if (string.IsNullOrEmpty(patch.Value) || patch.Value == "null")
                        {
                            physicalScore.Result = null;
                        }
                        else
                        {
                            physicalScore.Result = decimal.Parse(patch.Value);
                        }
                        break;
                    case "Rank":
                        physicalScore.Rank = patch.Value;
                        break;
                    case "Jiafen":
                        if (string.IsNullOrEmpty(patch.Value) || patch.Value == "null")
                        {
                            physicalScore.Jiafen = null;
                        }
                        else
                        {
                            physicalScore.Jiafen = decimal.Parse(patch.Value);
                        }
                        break;
                    case "SpecialType":
                        physicalScore.SpecialType = (SpecialScoreType)int.Parse(patch.Value);
                        break;
                    case "Modifier":
                        physicalScore.Modifier = patch.Value;
                        break;
                    case "UpdateDate":
                        physicalScore.UpdateDate = DateTime.Parse(patch.Value);
                        break;
                    case "Note":
                        physicalScore.Note = patch.Value;
                        break;
                    default:
                        physicalScore.SetProperty(patch.Key, patch.Value);
                        break;
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
    }
}
