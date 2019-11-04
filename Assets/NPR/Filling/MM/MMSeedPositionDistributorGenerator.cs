using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.NPR.Filling.Szecsi;
using Assets.Utils;
using MathNet.Numerics.Statistics;
using UnityEngine;

namespace Assets.NPR.Filling.MM
{
    public class MMSeedPositionDistributorGenerator
    {
        public PointsWithLastBits FindBestPointsDistributionBasedOn2D()
        {
            int dimension = 2;
            int k = 2;
            int cycleLength = (int) Mathf.Pow(k * 2, dimension);
            var distributionsTriesCount = 20;

            var szecsiCycleGen = new SzecsiCyclesGenerator(dimension, k);

            var startSeed = 626;
            Vector2 distanceMultipliers = new Vector3(1,6);

            var distributions = Enumerable.Range(0, distributionsTriesCount).Select(c =>
            {
                var seed = startSeed + c;
                var p = GenerateRandomPointsDistribution(seed, szecsiCycleGen, dimension, cycleLength);
                var rating = RateDistribution2D(p.Positions, distanceMultipliers);
                return new
                {
                    points = p,
                    rating,
                    seed 
                };
            }).ToList();
            var bestDistributionObj = distributions.OrderByDescending(c => c.rating).First();
            Debug.Log(bestDistributionObj.rating+"  "+bestDistributionObj.seed);
            var bestDistribution = bestDistributionObj.points;

            List<List<float>> positions = new List<List<float>>();
            List<List<int>> lastCycleBits = new List<List<int>>();
            for (int j = 0; j < bestDistribution.Positions.Count; j++)
            {
                for (int layerIndex = 0; layerIndex < 4; layerIndex++)
                {
                    var pos = bestDistribution.Positions[j];
                    float x = pos[0];
                    float y = pos[1];

                    var lastBits = bestDistribution.LastCycleBits[j];

                    positions.Add(new List<float>
                    {
                        x, y, ((float) layerIndex) / 4.0f + 1 / 8.0f
                    });
                    lastCycleBits.Add(new List<int>() {lastBits[0], lastBits[1], 0});
                }
            }

            var pointsWithLastBitsIn3D = new PointsWithLastBits()
            {
                Positions = positions,
                LastCycleBits = lastCycleBits
            };

            return pointsWithLastBitsIn3D;
        }

        private static PointsWithLastBits GenerateRandomPointsDistribution(int seed, SzecsiCyclesGenerator szecsiCycleGen, int dimension, int cycleLength)
        {
            var cycle = szecsiCycleGen.GenerateCycle(seed);
            var uniformGroup = szecsiCycleGen.GenerateUniformGroupFromCycle(cycle, (uint) dimension);
            return SzecsiCyclesGenerator.GeneratePointsFromUniformGroup(uniformGroup, dimension, cycleLength);
        }

        private float RateDistribution(List<List<float>> points, Vector3 distanceMultipliers)
        {
            var vp = points.Select(c => new Vector3(c[0], c[1], c[2])).ToList();
            var mirrorMargin = 0.1f;
            var mirroredPoints = new List<Vector3>();
            foreach (var p in vp)
            {
                var possibleCoords = new List<float>[3];
                for (int i = 0; i < 3; i++)
                {
                    possibleCoords[i] = new List<float>();
                    possibleCoords[i].Add(p[i]);
                    if (p[i] < mirrorMargin || p[i] > 1-mirrorMargin)
                    {
                        possibleCoords[i].Add(1 - p[i]);
                    }
                }

                foreach (var x in possibleCoords[0])
                {
                    foreach (var y in possibleCoords[0])
                    {
                        foreach (var z in possibleCoords[0])
                        {
                            mirroredPoints.Add(new Vector3(x, y, z));
                        }
                    }
                }
            }

            var minimumDistancesAverage = mirroredPoints.Select(
                c => mirroredPoints.Select(
                    r =>
                    {
                        var m = (c - r);
                        return VectorUtils.MemberwiseMultiply(m, distanceMultipliers).magnitude;
                    })
                        .Where(r => r > 0.00001) // remove distance to self
                        .Minimum())
                .Distinct() // remove 2 times existing point distances like A-B and B-A
                .OrderByDescending(c => c)
                .Take(5)
                .Average();
            return minimumDistancesAverage;
            // for each point find distance to closest
        }

        private float RateDistribution2D(List<List<float>> points, Vector2 distanceMultipliers)
        {
            var vp = points.Select(c => new Vector2(c[0], c[1])).ToList();
            var mirrorMargin = 0.5f;
            var mirroredPoints = new List<Vector2>();
            foreach (var p in vp)
            {
                var possibleCoords = new List<float>[2];
                for (int i = 0; i < 2; i++)
                {
                    possibleCoords[i] = new List<float>();
                    possibleCoords[i].Add(p[i]);
                    if (p[i] < mirrorMargin || p[i] > 1-mirrorMargin)
                    {
                        possibleCoords[i].Add(1 - p[i]);
                    }
                }

                foreach (var x in possibleCoords[0])
                {
                    foreach (var y in possibleCoords[0])
                    {
                            mirroredPoints.Add(new Vector2(x, y));
                    }
                }
            }

            var minimumDistancesAverage = mirroredPoints.Select(
                c => mirroredPoints.Select(
                    r =>
                    {
                        var m = (c - r);
                        return VectorUtils.MemberwiseMultiply(m, distanceMultipliers).magnitude;
                    })
                        .Where(r => r > 0.00001) // remove distance to self
                        .Minimum())
                .Distinct() // remove 2 times existing point distances like A-B and B-A
                .OrderByDescending(c => c)
                .Take(1) // once it was 5
                .Average();
            return minimumDistancesAverage;
            // for each point find distance to closest
        }

    }
}
