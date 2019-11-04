using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.Heightmaps.GRing
{
    public interface IGRingSurfaceProvider
    {
        Task<List<GRingSurfaceDetail>> ProvideSurfaceDetail();
    }
}