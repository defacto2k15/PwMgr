using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Ring2.RegionSpace
{
    public class AggregateRegionSpaceDistanceProvider : IRegionSpaceDistanceProvider
    {
        private readonly List<PolygonRegionSpaceDistanceProvider> _providers;

        public AggregateRegionSpaceDistanceProvider(List<PolygonRegionSpaceDistanceProvider> providers)
        {
            _providers = providers;
        }

        public float GetDistanceAt(Vector2 position)
        {
            var positiveDistances = _providers.Select(c => c.GetDistanceAt(position)).Where(c => c >= 0).ToList();
            if (!positiveDistances.Any())
            {
                return -1;
            }
            else
            {
                return positiveDistances.Min();
            }
        }
    }
}