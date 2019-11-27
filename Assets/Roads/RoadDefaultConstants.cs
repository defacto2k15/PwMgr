using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;

namespace Assets.Roads
{
    public static class RoadDefaultConstants
    {
        public static float MaxProximity = 5f;
        public static float MaxDelta = 10f;
        public static float StartSlopeProximity = 1.5f;
        public static float EndSlopeProximity = 3f;
        public static IntVector2 ProximityArraySize = new IntVector2(241, 241);
    }
}