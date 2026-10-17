// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using HH.GeneralLibrary.Core.DAL;
using HH.GeneralLibrary.Core.DAL.Linq;

namespace HH.TiYu.DAL
{
    public interface IBatchProvider<TInfo, TID> : IProvider<TInfo, TID> where TInfo : class, IEntity<TID>
    {
        CommandResult BatchAdd(List<TInfo> items, string projectID, ImportOption option);

        CommandResult BatchDelete(List<TInfo> items, string projectID);

        CommandResult BatchPatch(List<UpdateItem<TID>> patchs);
    }
}
