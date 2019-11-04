using UnityEngine;

namespace Assets.Ring2.RegionSpace
{
    public interface IRegionSpaceDistanceProvider
    {
        float GetDistanceAt(Vector2 position);
    }
}