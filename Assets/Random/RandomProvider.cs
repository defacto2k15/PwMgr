using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.TerrainMat.BiomeGen;
using UnityEngine;

namespace Assets.Random
{
    public class RandomProvider
    {
        private int _lastSeed;

        public RandomProvider(int seed = 123)
        {
            _lastSeed = seed;
        }

        public float NextValue
        {
            get
            {
                var rand = new System.Random(_lastSeed);
                var toReturn = (float) rand.NextDouble();
                _lastSeed = rand.Next();
                return toReturn;
            }
        }

        public double RandomGaussian(double mean, double stdDev)
        {
            double u1 = 1.0 - NextValue;
            double u2 = 1.0 - NextValue;
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2);
            double randNormal =
                mean + stdDev * randStdNormal;
            return randNormal;
        }

        public double RandomGaussian(RandomCharacteristics characteristics)
        {
            return RandomGaussian(characteristics.Mean, characteristics.StandardDeviation);
        }

        public float Next(float min, float max)
        {
            return min + (max - min) * NextValue;
        }

        public int NextWithMax(int min, int max)
        {
            var delta = max - min;
            if (delta == 0)
            {
                return min;
            }
            var rand = NextValue;
            return Mathf.RoundToInt(Mathf.Clamp(min + delta * rand, min, max));
        }
    }

    public class UnsafeRandomProvider
    {
        private int _lastSeed;
        private System.Random _random;

        public UnsafeRandomProvider(int seed = 123)
        {
            _lastSeed = seed;
            _random = new System.Random(seed);
        }

        public float NextValue => (float) _random.NextDouble();

        public double RandomGaussian(double mean, double stdDev)
        {
            double u1 = 1.0 - NextValue;
            double u2 = 1.0 - NextValue;
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2);
            double randNormal =
                mean + stdDev * randStdNormal;
            return randNormal;
        }

        public double RandomGaussian(RandomCharacteristics characteristics)
        {
            return RandomGaussian(characteristics.Mean, characteristics.StandardDeviation);
        }

        public float Next(float min, float max)
        {
            return min + (max - min) * NextValue;
        }

        public int NextWithMax(int min, int max)
        {
            var delta = max - min;
            if (delta == 0)
            {
                return min;
            }
            var rand = NextValue;
            return Mathf.RoundToInt(Mathf.Clamp(min + delta * rand, min, max));
        }
    }
}