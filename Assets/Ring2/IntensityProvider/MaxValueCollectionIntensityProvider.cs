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
    public class MaxValueCollectionIntensityProvider : IFabricRing2IntensityProvider
    {
        private List<IFabricRing2IntensityProvider> _providers;

        public MaxValueCollectionIntensityProvider(List<IFabricRing2IntensityProvider> providers)
        {
            _providers = providers;
        }

        public Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions, IRegionSpace region)
        {
            Preconditions.Fail("Cannot compute intensity from region");
            return null;
        }

        public async Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions)
        {
            var allIntensities =
                await TaskUtils.WhenAll(_providers.Select(c => c.RetriveIntensityAsync(queryPositions)));

            return Enumerable.Range(0, queryPositions.Count).Select(i => allIntensities.Select(list => list[i]).Max()).ToList();
        }

        public bool RequiresRegionToCompute()
        {
            return false;
        }
    }
}