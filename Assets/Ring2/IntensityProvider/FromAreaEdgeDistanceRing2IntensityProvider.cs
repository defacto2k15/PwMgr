using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.RegionSpace;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Ring2.IntensityProvider
{
    public class FromAreaEdgeDistanceRing2IntensityProvider : IFabricRing2IntensityProvider
    {
        private float _slopeStartDistance;
        private Ring2AreaDistanceDatabase _distanceDatabase;

        public FromAreaEdgeDistanceRing2IntensityProvider(float slopeStartDistance,
            Ring2AreaDistanceDatabase distanceDatabase)
        {
            _slopeStartDistance = slopeStartDistance;
            _distanceDatabase = distanceDatabase;
        }

        public Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions, IRegionSpace space)
        {
            var toResult = TaskUtils.MyFromResult(queryPositions.Select(c => CalculateIntensity(c, space)).ToList());
            return toResult;
        }

        private float CalculateIntensity(Vector2 position, IRegionSpace space)
        {
            var distance = _distanceDatabase.RetriveDistance(position, space);
            if (distance < 0)
            {
                return 0;
            }
            return Mathf.InverseLerp(0, _slopeStartDistance, (float) distance);
        }

        public Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions)
        {
            Preconditions.Fail("Need area to recalculate it");
            return null;
        }

        public bool RequiresRegionToCompute()
        {
            return true;
        }
    }
}