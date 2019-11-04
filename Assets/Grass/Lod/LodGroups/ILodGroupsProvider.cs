using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Lod
{
    interface ILodGroupsProvider
    {
        LodGroup GenerateLodGroup(MapAreaPosition position);
    }
}