using System.Collections.Generic;
using Assets.Grass2.IntensitySampling;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.PositionResolving
{
    public class SimpleRandomSamplerPositionResolver : IGrassPositionResolver
    {
        private readonly SimpleRandomSampler _simpleRandomSampler = new SimpleRandomSampler();

        public List<Vector2> ResolvePositions(
            MyRectangle generationArea,
            IIntensitySamplingProvider intensityProvider,
            float instancesPerUnitSquare
        )
        {
            MyProfiler.BeginSample("SimpleRandomSapler resolving");
            var area = generationArea.Area;
            var generationCount = (int) (instancesPerUnitSquare * area);
            var maxTries = generationCount; //(int) generationCount * (40f / 15f);

            var toReturn = _simpleRandomSampler.Generate(
                generationArea,
                generationCount,
                maxTries,
                0.1f, false, intensityProvider);
            MyProfiler.EndSample();
            return toReturn;
        }
    }
}