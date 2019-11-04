using System.Collections.Generic;
using Assets.Grass2.IntensitySampling;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.PositionResolving
{
    public class PoissonDiskSamplerPositionResolver : IGrassPositionResolver
    {
        private readonly MultiIntensityPoissonDiskSampler _multiIntenstiySampler =
            new MultiIntensityPoissonDiskSampler();

        private MyRange _exclusionRadiusRange;

        public PoissonDiskSamplerPositionResolver(MyRange exclusionRadiusRange)
        {
            _exclusionRadiusRange = exclusionRadiusRange;
        }

        public List<Vector2> ResolvePositions(
            MyRectangle generationArea,
            IIntensitySamplingProvider intensityProvider,
            float instancesPerUnitSquare
        )
        {
            MyProfiler.BeginSample("PoissonDisk ResolvePositions");
            var area = generationArea.Area;
            var generationCount = 7;
            var maxTries = area * 10;

            var toReturn = _multiIntenstiySampler.Generate(generationArea, generationCount, _exclusionRadiusRange,
                intensityProvider, (int) maxTries);
            MyProfiler.EndSample();
            return toReturn;
        }
    }
}