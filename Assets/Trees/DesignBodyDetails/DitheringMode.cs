using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;

namespace Assets.Trees.DesignBodyDetails
{
    public enum DitheringMode
    {
        DISABLED,
        FULL_DETAIL,
        REDUCED_DETAIL,
        BILLBOARD
    }

    public static class DitheringModeUtils
    {
        public static float RetriveDitheringModeIndex(DitheringMode mode)
        {
            switch (mode)
            {
                case DitheringMode.BILLBOARD:
                    return 3f;
                case DitheringMode.FULL_DETAIL:
                    return 2f;
                case DitheringMode.REDUCED_DETAIL:
                    return 1f;
                case DitheringMode.DISABLED:
                    return 0f;
            }
            Preconditions.Fail("Not expected dithering mode " + mode);
            return -1;
        }
    }
}