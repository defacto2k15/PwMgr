using System.Threading.Tasks;

namespace Assets.TerrainMat.Stain
{
    public interface IStainTerrainResourceGenerator
    {
        Task<StainTerrainResource> GenerateTerrainTextureDataAsync();
    }
}