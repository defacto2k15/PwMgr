using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public interface IAsyncRing1NodeListener
    {
        Task CreatedNewNodeAsync();
        Task DoNotDisplayAsync();
        Task UpdateAsync(Vector3 cameraPosition);
    }
}