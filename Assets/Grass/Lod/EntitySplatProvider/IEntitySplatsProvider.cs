using UnityEngine;

namespace Assets.Grass.Lod
{
    interface IEntitySplatsProvider
    {
        IEntitySplat GenerateGrassSplat(Vector3 position, Vector2 size, int lodLevel);
    }
}