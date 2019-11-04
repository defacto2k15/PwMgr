using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class TweakedThermalEroder
    {
        public void Erode(SimpleHeightArray inHeightArray, ThermalErosionConfiguration configuration)
        {
            var tParam = configuration.TParam;
            var cParam = configuration.CParam;
            var stepCount = configuration.StepCount;

            var finalDifferenceArray = new SimpleHeightArray(inHeightArray.Width, inHeightArray.Height);

            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                Parallel.For<SimpleHeightArray>(0, inHeightArray.Height,
                    new ParallelOptions {MaxDegreeOfParallelism = 4},
                    () => new SimpleHeightArray(inHeightArray.Width, inHeightArray.Height),
                    (y, loop, localDifferenceArray) =>
                    {
//                Parallel.For(0, inHeightArray.Height,new ParallelOptions { MaxDegreeOfParallelism = 3 }, y =>
//                for (int y = 0; y < inHeightArray.Height; y++)
//                {
                        for (int x = 0; x < inHeightArray.Width; x++)
                        {
                            var point = new IntVector2(x, y);
                            var thisValue = inHeightArray.GetValue(point);
                            var neighbours = configuration.NeighbourFinder.Find(inHeightArray, point).Select(n => new
                            {
                                point = n,
                                difference = thisValue - inHeightArray.GetValue(n)
                            }).ToList();

                            neighbours = neighbours.Where(c => c.difference < tParam && c.difference > 0).ToList();
                            if (!neighbours.Any())
                            {
                                continue;
                            }
                            var dTotal = neighbours.Sum(c => c.difference);
                            var dMax = neighbours.Max(c => c.difference);

                            foreach (var aNeighbour in neighbours)
                            {
                                var movedGround = cParam * (dMax) * (aNeighbour.difference / dTotal);

                                //finalDifferenceArray.AddValue(aNeighbour.point, movedGround);
                                //finalDifferenceArray.AddValue(point, -movedGround);

                                localDifferenceArray.AddValue(aNeighbour.point, movedGround);
                                localDifferenceArray.AddValue(point, -movedGround);
                            }
                        }
                        return localDifferenceArray;
                    },
                    (localDifferenceArray) => { finalDifferenceArray.SumValue(localDifferenceArray); });


                for (int y = 0; y < inHeightArray.Height; y++)
                {
                    for (int x = 0; x < inHeightArray.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        inHeightArray.AddValue(point, finalDifferenceArray.GetValue(point));

                        finalDifferenceArray.SetValue(point, 0);
                    }
                }
            }
        }
    }
}