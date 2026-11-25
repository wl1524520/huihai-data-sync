using System.Collections.Generic;
using System.Linq;
using HH.TiYu.BLL;
using HH.TiYu.Model;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.UI
{
    /// <summary>采集窗口共享的科目字典；不引入年度项目或学生名单。</summary>
    internal static class WifiCollectionContext
    {
        private static readonly object _SyncRoot = new object();
        private static Dictionary<int, PhysicalItem> _Items;

        internal static PhysicalItem FindPhysicalItem(int id)
        {
            lock (_SyncRoot)
            {
                if (_Items == null)
                {
                    var result = new PhysicalItemBLL(AppSettings.Current.ConnStr).GetItems(null);
                    if (result.Result == ResultCode.Successful)
                    {
                        var items = result.QueryObjects ?? new List<PhysicalItem>();
                        _Items = items.GroupBy(it => it.ID).ToDictionary(group => group.Key, group => group.First());
                    }
                }
                if (_Items == null) return null;
                PhysicalItem item;
                return _Items.TryGetValue(id, out item) ? item : null;
            }
        }
    }
}
