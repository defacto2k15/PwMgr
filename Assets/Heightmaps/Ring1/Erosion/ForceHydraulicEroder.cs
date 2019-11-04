using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
//public class ForceHydraulicEroder
//{
//    public void Erode(SimpleHeightArray heightMap, ForceHydraulicEroderConfiguration configuration)
//    {
//        var waterMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
//        var sedimentMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);

//        var sedimentAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
//        var waterAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);

//        var velocityMap = new SimpleVectorArray(heightMap.Width, heightMap.Height);
//        var velocityAdditionMap = new SimpleVectorArray(heightMap.Width, heightMap.Height);

//        float w0_WaterAmountToBeDistributed = configuration.w0_WaterAmountToBeDistributed;
//        float wMin_WaterAmountToDistributeOnLowestCells = configuration.wMin_WaterAmountToDistributeOnLowestCells;
//        float ke_ErosionFactor = configuration.ke_ErosionFactor;
//        float g_GravitationAcceleration = configuration.g_GravitationAcceleration;
//        float ka_waterGroundFriction = configuration.ka_waterGroundFriction;
//        float stepDeltaTime = configuration.StepDeltaTime;

//        Vector3 distanceMeasurmentRealisator = configuration.DistanceMeasurmentRealisator; 

//        ErodedNeighbourFinder neighbourFinder = configuration.NeighbourFinder;
//        int stepCount = configuration.StepCount;


//        for (int i = 0; i < stepCount; i++)
//        {
//            if (i == 0)
//            {
//                //DistributeWater();
//                // lets add water
//                for (int y = 0; y < heightMap.Height; y++)
//                {
//                    for (int x = 0; x < heightMap.Width; x++)
//                    {
//                        var point = new IntVector2(x, y);
//                        var height = heightMap.GetValue(point);
//                        var waterToAdd = wMin_WaterAmountToDistributeOnLowestCells +
//                                         w0_WaterAmountToBeDistributed * (height / 1);
//                        waterMap.AddValue(point, waterToAdd);
//                    }
//                }
//            }

//            //EvaporateWater();
//            for (int y = 0; y < heightMap.Height; y++)
//            {
//                for (int x = 0; x < heightMap.Width; x++)
//                {
//                    var point = new IntVector2(x, y);
//                    waterMap.SetValue(point, waterMap.GetValue(point)*ke_ErosionFactor);
//                }
//            }

//            for (int y = 0; y < heightMap.Height; y++)
//            {
//                for (int x = 0; x < heightMap.Width; x++)
//                {
//                    var point = new IntVector2(x, y);
//                    var pointTotalHeight = heightMap.GetValue(point) + waterMap.GetValue(point);
//                    var neighbours = neighbourFinder.Find(heightMap, point).Select(p => new
//                    {
//                        Point = p,
//                        HeightDifference = heightMap.GetValue(p) + waterMap.GetValue(p) - pointTotalHeight
//                    }).ToList();

//                    var higherNeighbours = neighbours.Where(c => c.HeightDifference > 0).ToList();
//                    foreach (var aNeighbour in higherNeighbours)
//                    {
//                        var nPoint = aNeighbour.Point;
//                        var nDifference = aNeighbour.HeightDifference;

//                        var xzDiff = nPoint - point;
//                        var neighbourToCurrentVec = VectorUtils.MemberwiseMultiply(new Vector3(xzDiff.X, nDifference, xzDiff.Y ), distanceMeasurmentRealisator);

//                        var sinAlpha = nDifference / neighbourToCurrentVec.magnitude;
//                        var nAcceleration = g_GravitationAcceleration * sinAlpha * neighbourToCurrentVec.normalized;

//                        var nVelocity = velocityMap.GetValue(point);
//                        var movedGroundVelocity = nVelocity * (1 - ka_waterGroundFriction) +
//                                                  nAcceleration * stepDeltaTime;


//                        MoveFromNeighbourToCurrent();
//                        CalculateSedimentCapacity_Decide()
//                    }
//                    if (thereIsWaterAndThereAreLowerNeighbours())
//                    {
//                        RemoveAllWater();
//                    }
//                    if (ThereIsNoLowerNeighbour())
//                    {
//                        DepositSediment();
//                    }
//                }
//            }
//        }
//    }

//    public void Erode2(SimpleHeightArray heightMap, ForceHydraulicEroderConfiguration configuration)
//    {
//        var waterMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
//        var sedimentMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);

//        var sedimentAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);
//        var waterAdditionMap = new SimpleHeightArray(heightMap.Width, heightMap.Height);

//        var velocityMap = new SimpleVectorArray(heightMap.Width, heightMap.Height);
//        var velocityAdditionMap = new SimpleVectorArray(heightMap.Width, heightMap.Height);

//        float w0_WaterAmountToBeDistributed = configuration.w0_WaterAmountToBeDistributed;
//        float wMin_WaterAmountToDistributeOnLowestCells = configuration.wMin_WaterAmountToDistributeOnLowestCells;
//        float ke_ErosionFactor = configuration.ke_ErosionFactor;
//        float g_GravitationAcceleration = configuration.g_GravitationAcceleration;
//        float ka_waterGroundFriction = configuration.ka_waterGroundFriction;
//        float stepDeltaTime = configuration.StepDeltaTime;

//        Vector3 distanceMeasurmentRealisator = configuration.DistanceMeasurmentRealisator; 

//        ErodedNeighbourFinder neighbourFinder = configuration.NeighbourFinder;
//        int stepCount = configuration.StepCount;


//        for (int i = 0; i < stepCount; i++)
//        {
//            if (i == 0)
//            {
//                //DistributeWater();
//                // lets add water
//                for (int y = 0; y < heightMap.Height; y++)
//                {
//                    for (int x = 0; x < heightMap.Width; x++)
//                    {
//                        var point = new IntVector2(x, y);
//                        var height = heightMap.GetValue(point);
//                        var waterToAdd = wMin_WaterAmountToDistributeOnLowestCells +
//                                         w0_WaterAmountToBeDistributed * (height / 1);
//                        waterMap.AddValue(point, waterToAdd);
//                    }
//                }
//            }

//            //EvaporateWater();
//            for (int y = 0; y < heightMap.Height; y++)
//            {
//                for (int x = 0; x < heightMap.Width; x++)
//                {
//                    var point = new IntVector2(x, y);
//                    waterMap.SetValue(point, waterMap.GetValue(point)*ke_ErosionFactor);
//                }
//            }

//            for (int y = 0; y < heightMap.Height; y++)
//            {
//                for (int x = 0; x < heightMap.Width; x++)
//                {
//                    var point = new IntVector2(x, y);
//                    var pointTotalHeight = heightMap.GetValue(point) + waterMap.GetValue(point);
//                    var neighbours = neighbourFinder.Find(heightMap, point).Select(p => new
//                    {
//                        Point = p,
//                        HeightDifference = heightMap.GetValue(p) + waterMap.GetValue(p) - pointTotalHeight
//                    }).ToList();
//                    if (!neighbours.Any())
//                    {
//                        continue;
//                    }

//                    var higherNeighbours = neighbours.Where(c => c.HeightDifference > 0).ToList();
//                    var cWater = waterMap.GetValue(point);

//                    var heightDifferenceSum = neighbours.Sum(c => c.HeightDifference);
//                    foreach (var aNeighbour in higherNeighbours)
//                    {
//                        var nPoint = aNeighbour.Point;
//                        var nDifference = aNeighbour.HeightDifference;

//                        var xzDiff = nPoint - point;
//                        var neighbourToCurrentVec = VectorUtils.MemberwiseMultiply(new Vector3(xzDiff.X, nDifference, xzDiff.Y ), distanceMeasurmentRealisator);

//                        var sinAlpha = nDifference / neighbourToCurrentVec.magnitude;
//                        var nAcceleration = g_GravitationAcceleration * sinAlpha * neighbourToCurrentVec.normalized;

//                        var nVelocity = velocityMap.GetValue(point);
//                        var movedGroundVelocity = nVelocity * (1 - ka_waterGroundFriction) +
//                                                  nAcceleration * stepDeltaTime;

//                        var deltaWater = cWater * (nDifference / heightDifferenceSum);

//                        var oldNWater = waterMap.GetValue(nPoint);
//                        waterMap.AddValue(nPoint, deltaWater);
//                        var newNWater = waterMap.GetValue(nPoint);

//                        var oldNSpeed = velocityMap.GetValue(point);
//                        var newNSpeed = (oldNWater * oldNSpeed + deltaWater * movedGroundVelocity) / newNWater;
//                        velocityMap.SetValue(point, newNSpeed);
//                    }


//                    if (thereIsWaterAndThereAreLowerNeighbours())
//                    {
//                        RemoveAllWater();
//                    }
//                    if (ThereIsNoLowerNeighbour())
//                    {
//                        DepositSediment();
//                    }
//                }
//            }
//        }
//    }

    public class ForceHydraulicEroderConfiguration
    {
        public float w0_WaterAmountToBeDistributed;
        public float wMin_WaterAmountToDistributeOnLowestCells;
        public float ke_ErosionFactor;
        public float g_GravitationAcceleration;
        public float ka_waterGroundFriction;
        public int StepCount { get; set; }
        public ErodedNeighbourFinder NeighbourFinder { get; set; }
        public Vector3 DistanceMeasurmentRealisator { get; set; }
        public float StepDeltaTime { get; set; }
    }
}