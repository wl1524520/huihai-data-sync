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
    public class PhysicalScoreBLL : BLLBase<Guid, PhysicalScore>
    {
        public PhysicalScoreBLL(string repoUri)
            : base(repoUri)
        {
        }

        public CommandResult UpdateUploadToCloudDate(ThirdSyncPlateForm pf, List<PhysicalScore> scores, DateTime dt)
        {
            List<UpdateItem<Guid>> list = scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = pf.ToString(),
                Value = dt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();
            list.AddRange(scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = string.Format("{0}_msg", pf.ToString()),
                Value = null
            }));
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(list);
            if (commandResult.Result == ResultCode.Successful)
            {
                scores.ForEach(delegate(PhysicalScore it)
                {
                    it.设置上传到平台时间(pf, dt);
                });
            }
            return commandResult;
        }

        public CommandResult ClearUploadToCloudDate(ThirdSyncPlateForm pf, List<PhysicalScore> scores)
        {
            List<UpdateItem<Guid>> list = scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = pf.ToString(),
                Value = null
            }).ToList();
            list.AddRange(scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = string.Format("{0}_msg", pf.ToString()),
                Value = null
            }).ToList());
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(list);
            if (commandResult.Result == ResultCode.Successful)
            {
                scores.ForEach(delegate(PhysicalScore it)
                {
                    it.设置上传到平台时间(pf, null);
                    it.设置上传到平台错误信息(pf, null);
                });
            }
            return commandResult;
        }

        public CommandResult UpdateUploadToCloudMsg(ThirdSyncPlateForm pf, List<PhysicalScore> scores, string msg)
        {
            List<UpdateItem<Guid>> patchs = scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = string.Format("{0}_msg", pf.ToString()),
                Value = msg
            }).ToList();
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(patchs);
            if (commandResult.Result == ResultCode.Successful)
            {
                scores.ForEach(delegate(PhysicalScore it)
                {
                    it.设置上传到平台错误信息(pf, msg);
                });
            }
            return commandResult;
        }

        public CommandResult UpdateUploadToCloudDateAndError(ThirdSyncPlateForm pf, List<PhysicalScore> scores, DateTime dt, string error)
        {
            List<UpdateItem<Guid>> list = scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = pf.ToString(),
                Value = dt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();
            list.AddRange(scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = string.Format("{0}_msg", pf.ToString()),
                Value = error
            }));
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(list);
            if (commandResult.Result == ResultCode.Successful)
            {
                scores.ForEach(delegate(PhysicalScore it)
                {
                    it.设置上传到平台时间(pf, dt);
                });
            }
            return commandResult;
        }

        public CommandResult UpdateUploadToCloudMsg(ThirdSyncPlateForm pf, List<PhysicalScore> scores, Dictionary<Guid, string> errors)
        {
            List<UpdateItem<Guid>> patchs = errors.Select((KeyValuePair<Guid, string> it) => new UpdateItem<Guid>
            {
                ID = it.Key,
                Key = string.Format("{0}_msg", pf.ToString()),
                Value = it.Value
            }).ToList();
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(patchs);
            if (commandResult.Result == ResultCode.Successful)
            {
                scores.ForEach(delegate(PhysicalScore it)
                {
                    it.设置上传到平台错误信息(pf, errors[it.ID]);
                });
            }
            return commandResult;
        }

        public CommandResult SetSpecialScoreType(List<PhysicalScore> scores, SpecialScoreType spt, string opt)
        {
            List<UpdateItem<Guid>> list = scores.Select(delegate(PhysicalScore it)
            {
                UpdateItem<Guid> obj = new UpdateItem<Guid>
                {
                    ID = it.ID,
                    Key = "SpecialType"
                };
                int num = (int)spt;
                obj.Value = num.ToString();
                return obj;
            }).ToList();
            list.AddRange(scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = "Modifier",
                Value = opt
            }));
            list.AddRange(scores.Select((PhysicalScore it) => new UpdateItem<Guid>
            {
                ID = it.ID,
                Key = "Result",
                Value = (SpecialScoreSetting.Current.GetValue(it.PhysicalItem, spt).HasValue ? SpecialScoreSetting.Current.GetValue(it.PhysicalItem, spt).ToString() : null)
            }));
            CommandResult commandResult = ProviderFactory.Create<IBatchProvider<PhysicalScore, Guid>>(base.RepoUri).BatchPatch(list);
            if (commandResult.Result == ResultCode.Successful)
            {
                scores.ForEach(delegate(PhysicalScore it)
                {
                    it.SpecialType = spt;
                    it.Modifier = opt;
                    it.Result = SpecialScoreSetting.Current.GetValue(it.PhysicalItem, spt);
                });
            }
            return commandResult;
        }
    }
}
