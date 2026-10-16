using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using HH.TiYu.Model;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;

namespace HH.TiYu.DAL.LinqProvider
{
    /// <summary>
    /// 原 BLL/UI 的二进制兼容入口。采集器不提供照片功能，也不需要 StudentPhoto 表。
    /// 查询返回空结果；写入明确返回失败，避免把未保存的照片误报为保存成功。
    /// </summary>
    public class StudentPhotoProvider : ProviderBase<StudentPhoto, Guid>, IBatchProvider<StudentPhoto, Guid>, IProvider<StudentPhoto, Guid>
    {
        private const string UnsupportedMessage = "汇海数据采集器不支持照片功能。";

        public StudentPhotoProvider(string connStr, MappingSource ms) : base(connStr, ms) { }

        protected override StudentPhoto GetingItemByID(Guid id, DataContext dc)
        {
            return null;
        }

        protected override List<StudentPhoto> GetingItems(DataContext dc, SearchCondition search)
        {
            return new List<StudentPhoto>();
        }

        protected override void InsertingItem(StudentPhoto info, DataContext dc)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        protected override void UpdatingItem(StudentPhoto newVal, StudentPhoto original, DataContext dc)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        protected override void DeletingItem(StudentPhoto info, DataContext dc)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public CommandResult BatchAdd(List<StudentPhoto> items, string projectID, ImportOption option)
        {
            return new CommandResult(ResultCode.Fail, UnsupportedMessage);
        }

        public CommandResult BatchDelete(List<StudentPhoto> items, string projectID)
        {
            return new CommandResult(ResultCode.Fail, UnsupportedMessage);
        }

        public CommandResult BatchPatch(List<UpdateItem<Guid>> patchs)
        {
            return new CommandResult(ResultCode.Fail, UnsupportedMessage);
        }
    }
}
