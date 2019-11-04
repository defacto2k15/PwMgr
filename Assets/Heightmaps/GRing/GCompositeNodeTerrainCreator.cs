using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Utils;
using Assets.Utils.MT;

namespace Assets.Heightmaps.GRing
{
    public class GCompositeNodeTerrainCreator : INewGRingListenersCreator
    {
        private List<INewGRingListenersCreator> _innerCreators;

        public GCompositeNodeTerrainCreator(List<INewGRingListenersCreator> innerCreators)
        {
            _innerCreators = innerCreators;
        }

        public IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod lod)
        {
            return new GCompositeRing2Node(_innerCreators.Select(c => c.CreateNewListener(node, lod)).ToList());
        }
    }

    public class GCompositeRing2Node : IAsyncGRingNodeListener
    {
        private List<IAsyncGRingNodeListener> _innerListeners;

        public GCompositeRing2Node(List<IAsyncGRingNodeListener> innerListeners)
        {
            _innerListeners = innerListeners;
        }

        public Task CreatedNewNodeAsync()
        {
            return TaskUtils.WhenAll(_innerListeners.Select(c => c.CreatedNewNodeAsync()));
        }

        public Task UpdateAsync()
        {
            return TaskUtils.WhenAll(_innerListeners.Select(c => c.UpdateAsync()));
        }

        public Task DoNotDisplayAsync()
        {
            return TaskUtils.WhenAll(_innerListeners.Select(c => c.DoNotDisplayAsync()));
        }

        public Task Destroy()
        {
            return TaskUtils.WhenAll(_innerListeners.Select(c => c.Destroy()));
        }
    }
}