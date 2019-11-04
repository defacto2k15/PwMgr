using System.Collections.Generic;
using Assets.Grass2.IntensitySampling;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Grass2.PositionResolving
{
    public interface IGrassPositionResolver
    {
        List<Vector2> ResolvePositions(MyRectangle generationArea,
            IIntensitySamplingProvider intensityProvider,
            float instancesPerUnitSquare);
    }
}