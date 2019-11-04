using System.Collections.Generic;

namespace Assets.Ring2.RuntimeManagementOtherThread
{
    public class OverseedPatchId
    {
        public List<uint> Ids;

        public bool IsFilled
        {
            get { return Ids != null; }
        }
    }
}