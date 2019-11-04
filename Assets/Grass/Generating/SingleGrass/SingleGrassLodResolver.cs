using Assets.Grass.Lod;
using UnityEngine;

namespace Assets.Grass.Generating.SingleGrass
{
    internal class SingleGrassLodResolver : IEntityLodResolver
    {
        // globalLod 0 - 10
        public int GetEntityLod(int globalLod)
        {
            var t = Mathf.InverseLerp(MyConstants.MIN_GLOBAL_LOD, MyConstants.MAX_GLOBAL_LOD, globalLod);
            return (int) Mathf.Round(Mathf.Lerp(MyConstants.MIN_SINGLE_GRASS_LOD, MyConstants.MAX_SINGLE_GRASS_LOD, t));
        }
    }
}