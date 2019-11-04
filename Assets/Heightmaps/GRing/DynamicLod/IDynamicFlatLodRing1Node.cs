using System.Threading.Tasks;

namespace Assets.Heightmaps.GRing.DynamicLod
{
    public interface IDynamicFlatLodRing1Node
    {
        Task CreateAsync(FlatLod lod);
        Task DoNotDisplayAsync();
    }
}