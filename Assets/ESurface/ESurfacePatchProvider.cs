using System.Linq;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.ESurface
{
    public class ESurfacePatchProvider
    {
        private GRing2PatchesCreatorProxy _patchesCreator;
        private Ring2PatchStamplingOverseerFinalizer _patchStamper;
        private MipmapExtractor _mipmapExtractor;
        private readonly int _mipmapLevelToExtract;

        public ESurfacePatchProvider(GRing2PatchesCreatorProxy patchesCreator, Ring2PatchStamplingOverseerFinalizer patchStamper, MipmapExtractor mipmapExtractor, int mipmapLevelToExtract)
        {
            _patchesCreator = patchesCreator;
            _patchStamper = patchStamper;
            _mipmapExtractor = mipmapExtractor;
            _mipmapLevelToExtract = mipmapLevelToExtract;
        }

        public ESurfaceTexturesPack ProvideSurfaceDetail(MyRectangle inGamePosition, FlatLod flatLod)
        {
            var devisedPatches = _patchesCreator.CreatePatchAsync(inGamePosition.ToRectangle(), flatLod.ScalarValue).Result;
            Preconditions.Assert(devisedPatches.Count <= 1,
                $"More than one patches created: {devisedPatches.Count}, rect is {inGamePosition}");
            if (!devisedPatches.Any())
            {
                return null;
            }

            Preconditions.Assert(devisedPatches.Count==1, "There are more than one devised patch. Exacly "+devisedPatches.Count);
            var onlyPatch = devisedPatches.First();
            var stampedSlice = _patchStamper.FinalizeGPatchCreation(onlyPatch, flatLod.ScalarValue).Result;
            onlyPatch.Destroy();
            if (stampedSlice != null)
            {
                if (_mipmapLevelToExtract != 0)
                {
                    var mipMappedMainTexture = _mipmapExtractor.ExtractMipmap(new TextureWithSize()
                    {
                        Size = stampedSlice.Resolution,
                        Texture = stampedSlice.ColorStamp
                    }, RenderTextureFormat.ARGB32, _mipmapLevelToExtract);
                    GameObject.Destroy(stampedSlice.ColorStamp);
                    GameObject.Destroy(stampedSlice.NormalStamp);

                    return new ESurfaceTexturesPack()
                    {
                        MainTexture = mipMappedMainTexture.Texture,
                        NormalTexture = stampedSlice.NormalStamp
                    };
                }
                else
                {
                    return new ESurfaceTexturesPack()
                    {
                        MainTexture = stampedSlice.ColorStamp,
                        NormalTexture = stampedSlice.NormalStamp
                    };
                }
            }

            return null;
        }
    }

}