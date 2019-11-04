using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Heightmaps.Ring1
{
    public static class Ring1TestDefaults
    {
        public static int MaxLodLevel = 5;

        public static Dictionary<float, int> PrecisionDistances =
            new Dictionary<float, int>
            {
                {10 * 50f, 5},
                {25 * 50f, 4},
                {50 * 50f, 3},
                {100 * 50f, 2},
                {200 * 50f, 1}
            };
    }
}