using System.Collections.Generic;
using Assets.Ring2.RegionSpace;
using Assets.TerrainMat;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using UnityEngine;

namespace Assets.Ring2.IntensityProvider
{
    public class Ring2AreaDistanceDatabase
    {
        private Dictionary<IRegionSpace, RegionSpaceDistanceCache> _distanceCaches =
            new Dictionary<IRegionSpace, RegionSpaceDistanceCache>();

        public float RetriveDistance(Vector2 position, IRegionSpace regionSpace)
        {
            RegionSpaceDistanceCache cache;
            if (_distanceCaches.ContainsKey(regionSpace))
            {
                cache = _distanceCaches[regionSpace];
            }
            else
            {
                cache = new RegionSpaceDistanceCache(regionSpace.DistanceProvider);
                _distanceCaches[regionSpace] = cache;
            }
            return cache.GetAtPosition(position);
        }


        private class RegionSpaceDistanceCache
        {
            private Dictionary<Vector2, float> _cache = new Dictionary<Vector2, float>();
            private IRegionSpaceDistanceProvider _distanceProvider;

            public RegionSpaceDistanceCache(IRegionSpaceDistanceProvider distanceProvider)
            {
                _distanceProvider = distanceProvider;
            }

            public float GetAtPosition(Vector2 position)
            {
                if (_cache.ContainsKey(position))
                {
                    return _cache[position];
                }
                float distance = _distanceProvider.GetDistanceAt(position);
                _cache[position] = distance;
                return distance;
            }
        }
    }
}