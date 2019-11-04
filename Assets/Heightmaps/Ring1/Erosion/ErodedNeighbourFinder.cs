using System;
using System.Collections.Generic;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class ErodedNeighbourFinder
    {
        private Func<SimpleHeightArray, IntVector2, List<IntVector2>> _findFunc;

        public ErodedNeighbourFinder(Func<SimpleHeightArray, IntVector2, List<IntVector2>> findFunc)
        {
            _findFunc = findFunc;
        }

        public List<IntVector2> Find(SimpleHeightArray inHeightArray, IntVector2 point)
        {
            return _findFunc(inHeightArray, point);
        }
    }
}