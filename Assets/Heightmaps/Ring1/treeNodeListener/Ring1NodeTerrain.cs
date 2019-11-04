using System;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public abstract class Ring1NodeTerrain : IAsyncGRingNodeListener
    {
        private readonly Ring1VisibilityTextureChangeGrabber _ring1VisibilityTextureChangeGrabber;
        protected readonly Ring1PaintingOrderGrabber OrderGrabber;
        protected readonly Ring1Node Ring1Node;
        private UInt32? _ring1TerrainId;
        protected GameObject ParentObject;

        public Ring1NodeTerrain(Ring1Node ring1Node,
            Ring1VisibilityTextureChangeGrabber ring1VisibilityTextureChangeGrabber,
            Ring1PaintingOrderGrabber orderGrabber,
            GameObject parentObject)
        {
            _ring1VisibilityTextureChangeGrabber = ring1VisibilityTextureChangeGrabber;
            Ring1Node = ring1Node;
            OrderGrabber = orderGrabber;
            ParentObject = parentObject;
        }

        public Task Destroy()
        {
            return TaskUtils.EmptyCompleted();
        }

        public Task UpdateAsync()
        {
            _ring1VisibilityTextureChangeGrabber.SetVisible(Ring1Node.Ring1Position);
            return TaskUtils.EmptyCompleted();
        }

        public Task DoNotDisplayAsync()
        {
            if (_ring1TerrainId.HasValue)
            {
                OrderGrabber.SetActive(_ring1TerrainId.Value, false);
            }
            return TaskUtils.EmptyCompleted();
        }

        protected abstract Task<UInt32> CreateTerrainAsync();

        public async Task CreatedNewNodeAsync()
        {
            _ring1TerrainId = await CreateTerrainAsync();
        }
    }
}