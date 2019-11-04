using System;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GVoidNodeTerrain : IAsyncGRingNodeListener
    {
        public Task CreatedNewNodeAsync()
        {
            return TaskUtils.EmptyCompleted();
        }

        public Task DoNotDisplayAsync()
        {
            return TaskUtils.EmptyCompleted();
        }

        public Task UpdateAsync()
        {
            return TaskUtils.EmptyCompleted();
        }

        public Task Destroy()
        {
            return TaskUtils.EmptyCompleted();
        }
    }

    public class GVoidNodeTerrainCreator : INewGRingListenersCreator
    {
        public IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod flatLod)
        {
            return new GVoidNodeTerrain();
        }
    }
}