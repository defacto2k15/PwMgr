using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class ThermalErosionGroundMover
    {
        private Action<List<ThermalEroder.NeighbourInfo>, ThermalErosionConfiguration, SimpleHeightArray, IntVector2>
            _func;

        public ThermalErosionGroundMover(
            Action<List<ThermalEroder.NeighbourInfo>, ThermalErosionConfiguration, SimpleHeightArray, IntVector2> func)
        {
            _func = func;
        }

        public void Move(List<ThermalEroder.NeighbourInfo> neighbours, ThermalErosionConfiguration configuration,
            SimpleHeightArray outArray, IntVector2 point)
        {
            _func(neighbours, configuration, outArray, point);
        }
    }

    public static class ThermalErosionGroundMovers
    {
        public static ThermalErosionGroundMover AllNeighboursMover = new ThermalErosionGroundMover(
            (neighbours, configuration, outArray, point) =>
            {
                var tParam = configuration.TParam;
                var cParam = configuration.CParam;

                var dMax = neighbours.Max(c => c.Difference);
                var dTotal = neighbours.Sum(c => c.Difference);

                foreach (var aNeighbour in neighbours)
                {
                    var movedGround = cParam * (dMax - tParam) * (aNeighbour.Difference / dTotal);

                    outArray.AddValue(aNeighbour.Point, movedGround);
                    outArray.AddValue(point, -movedGround);
                }
            });

        public static ThermalErosionGroundMover OnlyBestMover = new ThermalErosionGroundMover(
            (neighbours, configuration, outArray, point) =>
            {
                var tParam = configuration.TParam;
                var cParam = configuration.CParam;
                var bestNeighbour = neighbours.OrderByDescending(c => c.Difference).First();
                var movedGround = cParam * (bestNeighbour.Difference - tParam);

                outArray.AddValue(bestNeighbour.Point, movedGround);
                outArray.AddValue(point, -movedGround);
            });

        public static ThermalErosionGroundMover OnlyBestMoverTweaked = new ThermalErosionGroundMover(
            (neighbours, configuration, outArray, point) =>
            {
                var tParam = configuration.TParam;
                var cParam = configuration.CParam;
                var bestNeighbour = neighbours.OrderByDescending(c => c.Difference).First();
                var movedGround = cParam * (bestNeighbour.Difference);

                outArray.AddValue(bestNeighbour.Point, movedGround);
                outArray.AddValue(point, -movedGround);
            });
    }
}