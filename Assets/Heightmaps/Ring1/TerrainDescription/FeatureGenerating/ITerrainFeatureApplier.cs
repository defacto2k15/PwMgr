using System.Threading.Tasks;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public interface ITerrainFeatureApplier
    {
        Task<TextureWithCoords> ApplyFeatureAsync(TextureWithCoords texture, TerrainCardinalResolution resolution,
            bool canMultistep);
    }
}