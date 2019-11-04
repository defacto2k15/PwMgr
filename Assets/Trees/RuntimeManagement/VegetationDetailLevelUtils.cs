using System.Collections.Generic;
using Assets.Utils;

namespace Assets.Trees.RuntimeManagement
{
    public static class VegetationDetailLevelUtils
    {
        public static List<VegetationDetailLevel> AllFromSmallToBig()
        {
            return new List<VegetationDetailLevel>()
            {
                VegetationDetailLevel.FULL,
                VegetationDetailLevel.REDUCED,
                VegetationDetailLevel.BILLBOARD
            };
        }

        public static int GetLevelIdOffset(VegetationDetailLevel level)
        {
            if (level == VegetationDetailLevel.BILLBOARD)
            {
                return 330000;
            }
            if (level == VegetationDetailLevel.REDUCED)
            {
                return 5500000;
            }
            if (level == VegetationDetailLevel.FULL)
            {
                return 99000000;
            }
            Preconditions.Fail("Unnown level " + level);
            return 0;
        }
    }
}