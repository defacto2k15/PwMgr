using Assets.Random.Fields;
using UnityEngine;

namespace Assets.Grass2.IntensitySampling
{
    public class IntensityFromRandomFiguresCompositionProvider : IIntensitySamplingProvider
    {
        private IntensityFieldFigure _baseIntensityFieldFigure;
        private IntensityFieldFigure _additionalIntensityFieldFigure;
        private float _additionalWeight;

        public IntensityFromRandomFiguresCompositionProvider(IntensityFieldFigure baseIntensityFieldFigure,
            IntensityFieldFigure additionalIntensityFieldFigure, float additionalWeight)
        {
            _baseIntensityFieldFigure = baseIntensityFieldFigure;
            _additionalIntensityFieldFigure = additionalIntensityFieldFigure;
            _additionalWeight = additionalWeight;
        }

        public float Sample(Vector2 uv)
        {
            var baseIntensity = _baseIntensityFieldFigure.GetPixelWithUv(uv);
            var additionalIntensity = _additionalIntensityFieldFigure.GetPixelWithUv(uv);
            return Mathf.Clamp01(baseIntensity + (additionalIntensity - 0.5f) * _additionalWeight);
        }
    }
}