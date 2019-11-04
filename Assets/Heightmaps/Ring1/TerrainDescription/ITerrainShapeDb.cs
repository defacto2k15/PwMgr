using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public interface ITerrainShapeDb
    {
        Task<TerrainDescriptionOutput> Query(TerrainDescriptionQuery query);
        Task DisposeTerrainDetailElement(TerrainDetailElementToken token);
    }
}