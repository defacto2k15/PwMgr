using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Grass2.Growing;
using Assets.Grass2.IntenstityDb;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils.Spatial;

namespace Assets.Grass2.GrassIntensityMap
{
    public interface IGrassIntensityMapProvider
    {
        Task<UvdCoordedPart<List<Grass2TypeWithIntensity>>> ProvideMapsAtAsync(MyRectangle queryArea);
    }
}