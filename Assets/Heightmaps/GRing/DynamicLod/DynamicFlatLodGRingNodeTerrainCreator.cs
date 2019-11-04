using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.treeNodeListener;
using UnityEngine;

namespace Assets.Heightmaps.GRing.DynamicLod
{
    public class DynamicFlatLodGRingNodeTerrainCreator : INewQuadListenersCreator
    {
        private INewGRingListenersCreator _subCreator;
        private FlatLodCalculator _flatLodCalculator;

        public DynamicFlatLodGRingNodeTerrainCreator(INewGRingListenersCreator subCreator,
            FlatLodCalculator flatLodCalculator)
        {
            _subCreator = subCreator;
            _flatLodCalculator = flatLodCalculator;
        }

        public IAsyncRing1NodeListener CreateNewListener(Ring1Node node)
        {
            return new DynamicFlatLodGRingNodeTerrain(_subCreator, _flatLodCalculator, node);
        }
    }
}