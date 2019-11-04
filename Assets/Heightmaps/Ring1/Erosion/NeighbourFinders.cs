using System.Collections.Generic;
using System.Linq;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public static class NeighbourFinders
    {
        public static ErodedNeighbourFinder Big9Finder = new ErodedNeighbourFinder((heightArray, center) =>
        {
            var outNeighbours = new List<IntVector2>();
            var boundaries = heightArray.Boundaries;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var nx = center.X + x;
                    var ny = center.Y + y;

                    var neighbour = new IntVector2(nx, ny);
                    if (boundaries.AreValidIndexes(neighbour) && !center.Equals(new IntVector2(x, y)))
                    {
                        outNeighbours.Add(neighbour);
                    }
                }
            }
            return outNeighbours;
        });

        public static ErodedNeighbourFinder Cross4Finder = new ErodedNeighbourFinder((heightArray, center) =>
        {
            var heightArrayBoundaries = heightArray.Boundaries;
            var neighbours = new List<IntVector2>()
            {
                center - new IntVector2(-1, 0),
                center - new IntVector2(1, 0),
                center - new IntVector2(0, -1),
                center - new IntVector2(0, 1),
            }.Where(c => { return heightArrayBoundaries.AreValidIndexes(c); }).ToList();
            return neighbours;
        });

        public static ErodedNeighbourFinder X4Finder = new ErodedNeighbourFinder((heightArray, center) =>
        {
            var heightArrayBoundaries = heightArray.Boundaries;
            var neighbours = new List<IntVector2>()
            {
                center - new IntVector2(1, 1),
                center - new IntVector2(1, -1),
                center - new IntVector2(-1, -1),
                center - new IntVector2(-1, 1),
            }.Where(c => { return heightArrayBoundaries.AreValidIndexes(c); }).ToList();
            return neighbours;
        });
    }
}