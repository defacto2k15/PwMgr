using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Heightmaps
{
    public class HeightDenormalizer
    {
        private float _denormalizationMultiplier;
        private float _denormalizationOffset;

        public HeightDenormalizer(float denormalizationMultiplier, float denormalizationOffset)
        {
            _denormalizationMultiplier = denormalizationMultiplier;
            _denormalizationOffset = denormalizationOffset;
        }

        public float Denormalize(float input)
        {
            return input * _denormalizationMultiplier + _denormalizationOffset;
        }

        public float Normalize(float input)
        {
            return (input - _denormalizationOffset) / _denormalizationMultiplier;
        }

        public float DenormalizationMultiplier => _denormalizationMultiplier;

        public static HeightDenormalizer Default => new HeightDenormalizer(2385f, 197);
        public static HeightDenormalizer Identity => new HeightDenormalizer(1, 0);


        public float NormalizeLength(float input)
        {
            return input / _denormalizationMultiplier;
        }

        public float DenormalizationOffset => _denormalizationOffset;
    }
}