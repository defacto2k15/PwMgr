using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.RegionSpace;
using Assets.Utils;
using UnityEngine;

namespace Assets.Ring2.IntensityProvider
{
    public class RegionPinnedFabricRing2IntensityProvider : IFabricRing2IntensityProvider
    {
        private IFabricRing2IntensityProvider _intensityProvider;
        private IRegionSpace _region;

        public RegionPinnedFabricRing2IntensityProvider(IFabricRing2IntensityProvider intensityProvider,
            IRegionSpace region)
        {
            _intensityProvider = intensityProvider;
            _region = region;
        }

        public Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions, IRegionSpace region)
        {
            Preconditions.Fail("Arleady pinned region");
            return null;
        }

        public Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions)
        {
            var toReturn = _intensityProvider.RetriveIntensityAsync(queryPositions, _region);
            return toReturn;
        }

        public bool RequiresRegionToCompute()
        {
            return false;
        }
    }
}