using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.GRing.DynamicLod
{
    public class DynamicFlatLodGRingNodeTerrain : IAsyncRing1NodeListener
    {
        private INewGRingListenersCreator _listenersCreator;
        private FlatLodCalculator _flatLodCalculator;
        private Ring1Node _node;

        private FlatLod _nodeFlatLod = FlatLod.NOT_SET;
        private IAsyncGRingNodeListener _currentListener;

        public DynamicFlatLodGRingNodeTerrain(INewGRingListenersCreator listenersCreator,
            FlatLodCalculator flatLodCalculator, Ring1Node node)
        {
            _listenersCreator = listenersCreator;
            _flatLodCalculator = flatLodCalculator;
            _node = node;
        }


        public Task CreatedNewNodeAsync()
        {
            return TaskUtils.EmptyCompleted();
        }

        public Task DoNotDisplayAsync()
        {
            if (_currentListener != null)
            {
                return _currentListener.DoNotDisplayAsync();
            }
            else
            {
                return TaskUtils.EmptyCompleted();
            }
        }

        public async Task UpdateAsync(Vector3 cameraPosition)
        {
            if (_currentListener == null)
            {
                var flatLod = _flatLodCalculator.CalculateFlatLod(_node, cameraPosition);
                _currentListener = _listenersCreator.CreateNewListener(_node, flatLod);
                await _currentListener.CreatedNewNodeAsync();
            }
            await _currentListener.UpdateAsync();


            //var flatLod = _flatLodCalculator.CalculateFlatLod(_node, cameraPosition);
            //if (!flatLod.Equals(_nodeFlatLod))
            //{
            //    if (_currentListener != null)
            //    {
            //        await _currentListener.DoNotDisplayAsync();
            //    }
            //    _currentListener = _listenersCreator.CreateNewListener(_node, flatLod);
            //    await _currentListener.CreatedNewNodeAsync();
            //}
            //_nodeFlatLod = flatLod;
            //await _currentListener.UpdateAsync();
        }
    }
}