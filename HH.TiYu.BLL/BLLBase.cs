// 从 lib/BLL.dll 还原；维护范围与差异见 README.md。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using HH.TiYu.DAL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;

namespace HH.TiYu.BLL
{
    public abstract class BLLBase<TID, TEntity> where TEntity : class, IEntity<TID>
    {
        public string RepoUri { get; set; }

        public BLLBase(string repoUri)
        {
            RepoUri = repoUri;
        }

        public virtual QueryResult<TEntity> GetByID(TID id)
        {
            return ProviderFactory.Create<IProvider<TEntity, TID>>(RepoUri).GetByID(id);
        }

        public virtual QueryResultList<TEntity> GetItems(SearchCondition condition)
        {
            return ProviderFactory.Create<IProvider<TEntity, TID>>(RepoUri).GetItems(condition);
        }

        public virtual CommandResult Add(TEntity info)
        {
            return ProviderFactory.Create<IProvider<TEntity, TID>>(RepoUri).Insert(info);
        }

        public virtual CommandResult Update(TEntity info)
        {
            TEntity queryObject = ProviderFactory.Create<IProvider<TEntity, TID>>(RepoUri).GetByID(info.ID).QueryObject;
            if (queryObject != null)
            {
                return ProviderFactory.Create<IProvider<TEntity, TID>>(RepoUri).Update(info, queryObject);
            }
            return new CommandResult(ResultCode.Fail, "更新失败，记录被删除");
        }

        public virtual CommandResult Delete(TEntity info)
        {
            return ProviderFactory.Create<IProvider<TEntity, TID>>(RepoUri).Delete(info);
        }
    }
}
