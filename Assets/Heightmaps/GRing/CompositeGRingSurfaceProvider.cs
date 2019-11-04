using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;

namespace Assets.Heightmaps.GRing
{
    public class CompositeGRingSurfaceProvider : IGRingSurfaceProvider
    {
        private List<IGRingSurfaceProvider> _surfaceProviders;

        public CompositeGRingSurfaceProvider(List<IGRingSurfaceProvider> surfaceProviders)
        {
            _surfaceProviders = surfaceProviders;
        }

        public async Task<List<GRingSurfaceDetail>> ProvideSurfaceDetail()
        {
            return (await TaskUtils.WhenAll(_surfaceProviders.Select(c => c.ProvideSurfaceDetail()))).SelectMany(c => c)
                .ToList();
        }
    }
}