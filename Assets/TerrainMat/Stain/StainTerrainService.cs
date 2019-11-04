using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.TerrainMat.Stain
{
    public class StainTerrainService
    {
        private IStainTerrainResourceGenerator _generator;
        private MyRectangle _stainTerrainCoords;
        private StainTerrainResource _textures = null;
        private TaskCompletionSource<object> _texturesLoadCompleteTcs;

        public StainTerrainService(IStainTerrainResourceGenerator generator, MyRectangle stainTerrainCoords)
        {
            _generator = generator;
            _stainTerrainCoords = stainTerrainCoords;
        }

        public async Task<StainTerrainResourceWithCoords> RetriveResource(MyRectangle requestedArea)
        {
            if (_textures == null)
            {
                if (_texturesLoadCompleteTcs == null)
                {
                    _texturesLoadCompleteTcs = new TaskCompletionSource<object>();
                    _textures = await _generator.GenerateTerrainTextureDataAsync();
                    _texturesLoadCompleteTcs.SetResult(null);
                }
                else
                {
                    await _texturesLoadCompleteTcs.Task;
                }
            }

            var outCoords = RectangleUtils.CalculateSubelementUv(_stainTerrainCoords, requestedArea);
            AssertOutCoordsAreNormalized(outCoords);

            return new StainTerrainResourceWithCoords()
            {
                Coords = outCoords,
                Resource = _textures
            };
        }

        private void AssertOutCoordsAreNormalized(MyRectangle coords)
        {
            Preconditions.Assert(
                coords.X >= 0 && coords.Y >= 0 && coords.Width >= 0 && coords.Height >= 0,
                "RequestedArea was out of stainTerrainCoords");
        }
    }

    public class StainTerrainResourceWithCoords
    {
        public StainTerrainResource Resource;
        public MyRectangle Coords;
    }

    public class StainTerrainServiceProxy : BaseOtherThreadProxy
    {
        private StainTerrainService _stainTerrainService;

        public StainTerrainServiceProxy(StainTerrainService stainTerrainService) : base(
            "StainTerrainServiceProxyThread", false)
        {
            _stainTerrainService = stainTerrainService;
        }

        public Task<StainTerrainResourceWithCoords> RetriveResource(MyRectangle requestedArea)
        {
            var tcs = new TaskCompletionSource<StainTerrainResourceWithCoords>();
            PostAction(async () => { tcs.SetResult(await _stainTerrainService.RetriveResource(requestedArea)); });
            return tcs.Task;
        }
    }
}