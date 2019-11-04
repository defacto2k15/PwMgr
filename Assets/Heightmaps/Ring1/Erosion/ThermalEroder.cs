using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class ThermalEroder
    {
        public void Erode(SimpleHeightArray inHeightArray, ThermalErosionConfiguration configuration)
        {
            var tParam = configuration.TParam;
            var cParam = configuration.CParam;
            var stepCount = configuration.StepCount;
            var thermalErosionGroundMover = configuration.GroundMover;
            var neighbourChooser = configuration.NeighboursChooser;

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
                            var neighbours = configuration.NeighbourFinder.Find(inHeightArray, point)
                                .Select(n => new NeighbourInfo(n, thisValue - inHeightArray.GetValue(n))).ToList();

                            neighbours = neighbours.Where(c => neighbourChooser.Choose(c, configuration)).ToList();
                            if (!neighbours.Any())
                            {
                                continue;
                            }

                            var changeArray = localDifferenceArray;
                            //changeArray = finalDifferenceArray;
                            thermalErosionGroundMover.Move(neighbours, configuration, changeArray, point);
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

        public class NeighbourInfo
        {
            public IntVector2 Point;
            public float Difference;

            public NeighbourInfo(IntVector2 point, float difference)
            {
                Point = point;
                Difference = difference;
            }
        }

        public void AtOnceErode(SimpleHeightArray inHeightArray, ThermalErosionConfiguration configuration)
        {
            var tParam = configuration.TParam;
            var cParam = configuration.CParam;
            var stepCount = configuration.StepCount;

            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                for (int y = 0; y < inHeightArray.Height; y++)
                {
                    for (int x = 0; x < inHeightArray.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        var thisValue = inHeightArray.GetValue(point);
                        var neighbours = configuration.NeighbourFinder.Find(inHeightArray, point).Select(n => new
                        {
                            point = n,
                            difference = thisValue - inHeightArray.GetValue(n)
                        }).ToList();

                        neighbours = neighbours.Where(c => c.difference > tParam).ToList();
                        if (!neighbours.Any())
                        {
                            continue;
                        }
                        var dTotal = neighbours.Sum(c => c.difference);
                        var dMax = neighbours.Max(c => c.difference);

                        foreach (var aNeighbour in neighbours)
                        {
                            var movedGround = cParam * (dMax - tParam) * (aNeighbour.difference / dTotal);
                            inHeightArray.AddValue(aNeighbour.point, movedGround);
                            inHeightArray.AddValue(point, -movedGround);
                        }
                    }
                }
            }
        }
    }

    public class ThermalErosionConfiguration
    {
        public float TParam = 0.0005f;
        public float CParam = 0.4f;
        public float StepCount = 20;
        public ErodedNeighbourFinder NeighbourFinder = NeighbourFinders.Big9Finder;
        public ThermalErosionGroundMover GroundMover = ThermalErosionGroundMovers.AllNeighboursMover;
        public ThermalEroderNeighboursChooser NeighboursChooser = ThermalEroderNeighboursChoosers.BiggerThanTChooser;
    }

    public class ThermalEroderNeighboursChooser
    {
        private Func<ThermalEroder.NeighbourInfo, ThermalErosionConfiguration, bool> _func;

        public ThermalEroderNeighboursChooser(Func<ThermalEroder.NeighbourInfo, ThermalErosionConfiguration, bool> func)
        {
            _func = func;
        }

        public bool Choose(ThermalEroder.NeighbourInfo neighbourInfo, ThermalErosionConfiguration conf)
        {
            return _func(neighbourInfo, conf);
        }
    }

    public static class ThermalEroderNeighboursChoosers
    {
        public static ThermalEroderNeighboursChooser BiggerThanTChooser = new ThermalEroderNeighboursChooser(
            (neighbour, configuration) => { return neighbour.Difference > configuration.TParam; });

        public static ThermalEroderNeighboursChooser LesserEqualThanTChooser = new ThermalEroderNeighboursChooser(
            (neighbour, configuration) => { return neighbour.Difference <= configuration.TParam; });
    }
}