using System.Collections.Generic;

namespace Assets.Ring2.RuntimeManagementOtherThread
{
    public class Ring2PatchesOverseerOrder
    {
        public List<Ring2PatchesCreationOrderElement> CreationOrder;
        public List<OverseedPatchId> RemovalOrders;

        public Ring2PatchesOverseerOrder()
        {
            CreationOrder = new List<Ring2PatchesCreationOrderElement>();
            RemovalOrders = new List<OverseedPatchId>();
        }
    }
}