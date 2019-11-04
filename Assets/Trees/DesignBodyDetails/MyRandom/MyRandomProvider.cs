using System.Collections.Generic;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails.MyRandom
{
    public class MyRandomProvider
    {
        private readonly bool _isUnsafe;
        private int _globalSeed;
        private List<StringSeed> _usedSeeds;

        private System.Random _unsafeRandom;


        public MyRandomProvider(int globalSeed, bool isUnsafe = false)
        {
            _globalSeed = globalSeed;
            _isUnsafe = isUnsafe;
            if (!_isUnsafe)
            {
                _usedSeeds = new List<StringSeed>();
            }
            else
            {
                _unsafeRandom = new System.Random(globalSeed);
            }
        }

        public void SetGlobalSeed(int globalSeed)
        {
            _globalSeed = globalSeed;
            if (_isUnsafe)
            {
                _unsafeRandom = new System.Random(globalSeed);
            }
        }

        public void Reset()
        {
            if (!_isUnsafe)
            {
                _usedSeeds.Clear();
            }
        }

        public float FloatValue(StringSeed seed)
        {
            var random = GenerateRandomProvider(seed);
            return (float) random.NextDouble();
        }

        public float FloatValueRange(StringSeed seed, float min, float max)
        {
            var floatVal = FloatValue(seed);
            return floatVal * (max - min) + min;
        }

        public int IntValue(StringSeed seed)
        {
            var random = GenerateRandomProvider(seed);
            return random.Next();
        }

        public int IntValueRange(StringSeed seed, int min, int max)
        {
            var random = GenerateRandomProvider(seed);
            return random.Next(min, max);
        }

        private System.Random GenerateRandomProvider(StringSeed seed)
        {
            if (_isUnsafe)
            {
                return _unsafeRandom;
            }
            Preconditions.Assert(!_usedSeeds.Contains(seed), "Seed, " + seed + " was arleady used");
            var provider = new System.Random(_globalSeed + seed.GetHashCode());
            _usedSeeds.Add(seed);
            return provider;
        }

        public int NextWithMax(StringSeed seed, int min, int max)
        {
            var delta = max - min;
            if (delta == 0)
            {
                return min;
            }
            var rand = (float)GenerateRandomProvider(seed).NextDouble();
            return Mathf.RoundToInt(Mathf.Clamp(min + delta * rand, min, max));
        }
    }

    public enum StringSeed
    {
        YRotation,
        HeightScale,
        ColorPackIndex,
        CombinationIdx,
        ColorIndex,
        HSV_H,
        HSV_S
    }
}