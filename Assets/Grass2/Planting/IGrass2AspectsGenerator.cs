using UnityEngine;

namespace Assets.Grass2.Planting
{
    public interface IGrass2AspectsGenerator
    {
        Grass2Aspect GenerateAspect(UnplantedGrassInstance unplantedInstance, Vector2 flatPosition);
    }
}