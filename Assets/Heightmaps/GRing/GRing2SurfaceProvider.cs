using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GRing2SurfaceProvider : IGRingSurfaceProvider
    {
        private GRing2PatchesCreatorProxy _patchesCreator;
        private readonly MyRectangle _inGamePosition;
        private readonly FlatLod _flatLod;

        public GRing2SurfaceProvider(GRing2PatchesCreatorProxy patchesCreator, MyRectangle inGamePosition,
            FlatLod flatLod)
        {
            _patchesCreator = patchesCreator;
            _inGamePosition = inGamePosition;
            _flatLod = flatLod;
        }

        public async Task<List<GRingSurfaceDetail>> ProvideSurfaceDetail()
        {
            var devisedPatches =
                await _patchesCreator.CreatePatchAsync(_inGamePosition.ToRectangle(), _flatLod.ScalarValue);
            Preconditions.Assert(devisedPatches.Count <= 1,
                $"More than one patches created: {devisedPatches.Count}, rect is {_inGamePosition}");
            if (!devisedPatches.Any())
            {
                return new List<GRingSurfaceDetail>();
            }

            var onlyPatch = devisedPatches.First();
            return onlyPatch.SliceInfos.Select(c => new GRingSurfaceDetail()
            {
                ShaderName = "Custom/Terrain/Ring2",
                UniformsWithKeywords = c,
            }).ToList();
        }
    }
}