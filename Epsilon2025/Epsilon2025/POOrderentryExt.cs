using PX.Data;
using PX.Objects.PO;
using System.Collections;

namespace Epsilon2025
{
    public class POOrderentryExt : PXGraphExtension<POOrderEntry>
    {
        public PXAction<POOrder> Sync;

        [PXButton(CommitChanges = true), PXUIField(DisplayName = "Sync Order", MapEnableRights = PXCacheRights.Select)]
        public virtual IEnumerable sync(PXAdapter adapter)
        {

            return adapter.Get();
        }
    }
}
