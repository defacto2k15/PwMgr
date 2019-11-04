using System.Threading.Tasks;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public interface IAsyncGRingNodeListener
    {
        Task CreatedNewNodeAsync();
        Task UpdateAsync();
        Task DoNotDisplayAsync();
        Task Destroy();
    }
}