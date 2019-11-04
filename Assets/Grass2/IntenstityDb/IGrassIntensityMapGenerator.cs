using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.Grass2.IntenstityDb
{
    public interface IGrassIntensityMapGenerator
    {
        Task<List<Grass2TypeWithIntensity>> GenerateMapsAsync(MyRectangle queryArea);
    }
}