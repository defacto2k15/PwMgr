using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Pathfinding.Fitting;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using Assets.Utils.Textures;
using UnityEngine;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Roads
{
    public class PathProximityTextureDbProxy : BaseOtherThreadProxy
    {
        private readonly SpatialDb<TextureWithSize> _proximityTexturesDb;

        public PathProximityTextureDbProxy(SpatialDb<TextureWithSize> proximityTexturesDb) : base(
            "PathProximityTextureDbProxyThread", false)
        {
            this._proximityTexturesDb = proximityTexturesDb;
        }

        public Task<UvdSizedTexture> Query(MyRectangle queryArea)
        {
            var tcs = new TaskCompletionSource<UvdSizedTexture>();
            PostAction(async () => tcs.SetResult(await ProvidePartInternal(queryArea)));
            return tcs.Task;
        }

        private async Task<UvdSizedTexture> ProvidePartInternal(MyRectangle queryArea)
        {
            var standardOut = await _proximityTexturesDb.ProvidePartsAt(queryArea);
            return new UvdSizedTexture()
            {
                TextureWithSize = standardOut.CoordedPart.Part,
                Uv = standardOut.Uv
            };
        }
    }
}