using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class HydraulicEroder
    {
        public void Erode(SimpleHeightArray heightMap, HydraulicEroderConfiguration configuration)
        {
            float krParam = configuration.kr_ConstantWaterAddition;
            float ksParam = configuration.ks_GroundToSedimentFactor;
            float keParam = configuration.ke_WaterEvaporationFactor;
            float kcParam = configuration.kc_MaxSedimentationFactor;
            bool finalSedimentationToGround = configuration.FinalSedimentationToGround;
            int stepCount = configuration.StepCount;
            ErodedNeighbourFinder neighbourFinder = configuration.NeighbourFinder;

            var waterMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var sedimentMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var sedimentAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var waterAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);

            for (int i = 0; i < stepCount; i++)
            {
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        if (configuration.WaterGenerator == HydraulicEroderWaterGenerator.FirstFrame)
                        {
                            if (i == 0)
                            {
                                waterMap.AddValue(point, krParam);
                            }
                        }
                        else
                        {
                            waterMap.AddValue(point, krParam);
                        }

                        float amountChangedToSediment = ksParam * waterMap.GetValue(point);
                        heightMap.AddValue(point, -amountChangedToSediment);
                        sedimentMap.AddValue(point, amountChangedToSediment);
                    }
                }

                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);

                        var neighbourPoints = neighbourFinder.Find(heightMap, point);

                        var pTotalHeight = heightMap.GetValue(point) + waterMap.GetValue(point);

                        var neighbours = neighbourPoints.Select(nPoint => new
                        {
                            Point = nPoint,
                            Ground = heightMap.GetValue(nPoint),
                            Water = waterMap.GetValue(nPoint),
                            TotalHeight = heightMap.GetValue(nPoint) + waterMap.GetValue(nPoint),
                            TotalHeightDifference = pTotalHeight - (heightMap.GetValue(nPoint) +
                                                                    waterMap.GetValue(nPoint)),
                        }).Where(c => c.TotalHeightDifference > 0).ToList();
                        if (!neighbours.Any())
                        {
                            continue;
                        }

                        var dTotalHeightDiffSum = neighbours.Sum(c => c.TotalHeightDifference);
                        var avgTotalHeight = (neighbours.Sum(c => c.TotalHeight) + pTotalHeight) /
                                             (neighbours.Count + 1);

                        var pSediment = sedimentMap.GetValue(point);
                        var pWater = waterMap.GetValue(point);
                        if (pWater < 0.001f)
                        {
                            continue;
                        }
                        var pDeltaA = pTotalHeight - avgTotalHeight;
                        var pMin = Mathf.Min(pDeltaA, pWater);

                        if (configuration.DestinationFinder == HydraulicEroderWaterDestinationFinder.OnlyBest)
                        {
                            var bestNeighbour = neighbours.OrderByDescending(c => c.TotalHeightDifference).First();
                            var nMovedWater = pMin;
                            var addedSediment = pSediment * (nMovedWater / pWater);
                            waterAdditionMap.AddValue(bestNeighbour.Point, nMovedWater);
                            sedimentAdditionMap.AddValue(bestNeighbour.Point, addedSediment);

                            sedimentAdditionMap.AddValue(point, -addedSediment);
                            waterAdditionMap.AddValue(point, -pMin);
                        }
                        else
                        {
                            var movedSedimentSum = 0f;
                            foreach (var aNeighbour in neighbours)
                            {
                                var nMovedWater = pMin * (aNeighbour.TotalHeightDifference / dTotalHeightDiffSum);
                                var addedSediment = pSediment * (nMovedWater / pWater);
                                movedSedimentSum += addedSediment;
                                waterAdditionMap.AddValue(aNeighbour.Point, nMovedWater);
                                sedimentAdditionMap.AddValue(aNeighbour.Point, addedSediment);
                            }
                            sedimentAdditionMap.AddValue(point, -movedSedimentSum);
                            waterAdditionMap.AddValue(point, -pMin);
                        }
                    }
                }

                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        waterMap.AddValue(point, waterAdditionMap.GetValue(point));
                        waterAdditionMap.SetValue(point, 0);

                        sedimentMap.AddValue(point, sedimentAdditionMap.GetValue(point));
                        sedimentAdditionMap.SetValue(point, 0);
                    }
                }

                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        var pWater = waterMap.GetValue(point);
                        var waterAfterEvaporation = pWater * (1 - keParam);
                        waterMap.SetValue(point, waterAfterEvaporation);

                        var pSedimentMax = kcParam * waterAfterEvaporation;
                        var pSediment = sedimentMap.GetValue(point);
                        var deltaSediment = Mathf.Max(0, pSediment - pSedimentMax);
                        sedimentMap.AddValue(point, -deltaSediment);
                        heightMap.AddValue(point, deltaSediment);
                    }
                }
            }

            if (finalSedimentationToGround)
            {
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        heightMap.AddValue(point, sedimentMap.GetValue(point));
                    }
                }
            }
        }
    }

    public class DebuggableHydraulicEroderOutput
    {
        public List<SimpleHeightArray> WaterSnapshots;
        public List<SimpleHeightArray> SedimentSnapshots;
    }

    public class DebuggableHydraulicEroder
    {
        public DebuggableHydraulicEroderOutput Erode(SimpleHeightArray heightMap,
            HydraulicEroderConfiguration configuration, int snapshotFrequencies)
        {
            List<SimpleHeightArray> WaterSnapshots = new List<SimpleHeightArray>();
            List<SimpleHeightArray> SedimentSnapshots = new List<SimpleHeightArray>();


            float krParam = configuration.kr_ConstantWaterAddition;
            float ksParam = configuration.ks_GroundToSedimentFactor;
            float keParam = configuration.ke_WaterEvaporationFactor;
            float kcParam = configuration.kc_MaxSedimentationFactor;
            bool finalSedimentationToGround = configuration.FinalSedimentationToGround;
            int stepCount = configuration.StepCount;
            ErodedNeighbourFinder neighbourFinder = configuration.NeighbourFinder;

            var waterMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var sedimentMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var sedimentAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
            var waterAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);

            for (int i = 0; i < stepCount; i++)
            {
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        if (configuration.WaterGenerator == HydraulicEroderWaterGenerator.FirstFrame)
                        {
                            if (i == 0)
                            {
                                waterMap.AddValue(point, krParam);
                            }
                        }
                        else
                        {
                            waterMap.AddValue(point, krParam);
                        }

                        float amountChangedToSediment = ksParam * waterMap.GetValue(point);
                        heightMap.AddValue(point, -amountChangedToSediment);
                        sedimentMap.AddValue(point, amountChangedToSediment);
                    }
                }

                if (i == 0)
                {
                    WaterSnapshots.Add(new SimpleHeightArray(MyArrayUtils.DeepClone<float>(waterMap.Array)));
                    SedimentSnapshots.Add(new SimpleHeightArray(MyArrayUtils.DeepClone<float>(sedimentMap.Array)));
                }


                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);

                        var neighbourPoints = neighbourFinder.Find(heightMap, point);

                        var pTotalHeight = heightMap.GetValue(point) + waterMap.GetValue(point);

                        var neighbours = neighbourPoints.Select(nPoint => new
                        {
                            Point = nPoint,
                            TotalHeight = heightMap.GetValue(nPoint) + waterMap.GetValue(nPoint),
                            TotalHeightDifference = pTotalHeight - (heightMap.GetValue(nPoint) +
                                                                    waterMap.GetValue(nPoint)),
                        }).Where(c => c.TotalHeightDifference > 0).ToList();
                        if (!neighbours.Any())
                        {
                            continue;
                        }

                        var dTotalHeightDiffSum = neighbours.Sum(c => c.TotalHeightDifference);
                        var avgTotalHeight = (neighbours.Sum(c => c.TotalHeight) + pTotalHeight) /
                                             (neighbours.Count + 1);

                        var pSediment = sedimentMap.GetValue(point);
                        var pWater = waterMap.GetValue(point);
                        if (pWater < 0.00000001f)
                        {
                            continue;
                        }
                        var pDeltaA = pTotalHeight - avgTotalHeight;
                        var pMin = Mathf.Min(pDeltaA, pWater);

                        if (configuration.DestinationFinder == HydraulicEroderWaterDestinationFinder.OnlyBest)
                        {
                            var bestNeighbour = neighbours.OrderByDescending(c => c.TotalHeightDifference).First();
                            var nMovedWater = pMin;
                            var addedSediment = pSediment * (nMovedWater / pWater);
                            waterAdditionMap.AddValue(bestNeighbour.Point, nMovedWater);
                            sedimentAdditionMap.AddValue(bestNeighbour.Point, addedSediment);

                            sedimentAdditionMap.AddValue(point, -addedSediment);
                            waterAdditionMap.AddValue(point, -pMin);
                        }
                        else
                        {
                            var movedSedimentSum = 0f;
                            foreach (var aNeighbour in neighbours)
                            {
                                var nMovedWater = pMin * (aNeighbour.TotalHeightDifference / dTotalHeightDiffSum);
                                var addedSediment = pSediment * (nMovedWater / pWater);
                                movedSedimentSum += addedSediment;
                                waterAdditionMap.AddValue(aNeighbour.Point, nMovedWater);
                                sedimentAdditionMap.AddValue(aNeighbour.Point, addedSediment);
                            }
                            sedimentAdditionMap.AddValue(point, -movedSedimentSum);
                            waterAdditionMap.AddValue(point, -pMin);
                        }
                    }
                }

                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        waterMap.AddValue(point, waterAdditionMap.GetValue(point));
                        waterAdditionMap.SetValue(point, 0);

                        sedimentMap.AddValue(point, sedimentAdditionMap.GetValue(point));
                        sedimentAdditionMap.SetValue(point, 0);
                    }
                }

                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        var pWater = waterMap.GetValue(point);
                        var waterAfterEvaporation = pWater * (1 - keParam);
                        waterMap.SetValue(point, waterAfterEvaporation);

                        var pSedimentMax = kcParam * waterAfterEvaporation;
                        var pSediment = sedimentMap.GetValue(point);
                        var deltaSediment = Mathf.Max(0, pSediment - pSedimentMax);
                        sedimentMap.AddValue(point, -deltaSediment);
                        heightMap.AddValue(point, deltaSediment);
                    }
                }

                if (i % snapshotFrequencies == 0)
                {
                    WaterSnapshots.Add(new SimpleHeightArray(MyArrayUtils.DeepClone<float>(waterMap.Array)));
                    SedimentSnapshots.Add(new SimpleHeightArray(MyArrayUtils.DeepClone<float>(sedimentMap.Array)));
                }
            }

            if (finalSedimentationToGround)
            {
                for (int y = 0; y < heightMap.Height; y++)
                {
                    for (int x = 0; x < heightMap.Width; x++)
                    {
                        var point = new IntVector2(x, y);
                        heightMap.AddValue(point, sedimentMap.GetValue(point));
                    }
                }
            }
            return new DebuggableHydraulicEroderOutput()
            {
                SedimentSnapshots = SedimentSnapshots,
                WaterSnapshots = WaterSnapshots
            };
        }
    }


    public class HydraulicEroderConfiguration
    {
        public float kr_ConstantWaterAddition = 0.01f;
        public float ks_GroundToSedimentFactor = 0.01f;
        public float ke_WaterEvaporationFactor = 0.5f;
        public float kc_MaxSedimentationFactor = 0.01f;
        public int StepCount = 10;
        public ErodedNeighbourFinder NeighbourFinder = NeighbourFinders.Big9Finder;
        public bool FinalSedimentationToGround = false;
        public HydraulicEroderWaterDestinationFinder DestinationFinder = HydraulicEroderWaterDestinationFinder.All;
        public HydraulicEroderWaterGenerator WaterGenerator = HydraulicEroderWaterGenerator.AllFrames;
    }

    public enum HydraulicEroderWaterDestinationFinder
    {
        OnlyBest,
        All
    }

    public enum HydraulicEroderWaterGenerator
    {
        AllFrames,
        FirstFrame
    }
}