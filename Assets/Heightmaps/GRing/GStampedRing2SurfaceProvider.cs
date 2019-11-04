using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GStampedRing2SurfaceProvider : IGRingSurfaceProvider
    {
        private GRing2PatchesCreatorProxy _patchesCreator;
        private readonly MyRectangle _inGamePosition;
        private Ring2PatchStamplingOverseerFinalizer _patchStamper;
        private readonly FlatLod _flatLod;

        public GStampedRing2SurfaceProvider(GRing2PatchesCreatorProxy patchesCreator,
            MyRectangle inGamePosition, Ring2PatchStamplingOverseerFinalizer patchStamper, FlatLod flatLod)
        {
            _patchesCreator = patchesCreator;
            _inGamePosition = inGamePosition;
            _patchStamper = patchStamper;
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
            var stampedSlice = await _patchStamper.FinalizeGPatchCreation(onlyPatch, _flatLod.ScalarValue);
            if (stampedSlice != null)
            {
                UniformsPack pack = new UniformsPack();
                pack.SetTexture("_MainTex", stampedSlice.ColorStamp);
                pack.SetTexture("_NormalTex", stampedSlice.NormalStamp);

                var uniforms = new UniformsWithKeywords()
                {
                    Uniforms = pack,
                    Keywords = new ShaderKeywordSet()
                };

                return new List<GRingSurfaceDetail>()
                {
                    new GRingSurfaceDetail()
                    {
                        ShaderName = "Custom/Terrain/Ring2Stamped",
                        UniformsWithKeywords = uniforms
                    }
                };
            }

            return new List<GRingSurfaceDetail>();
        }
    }
}