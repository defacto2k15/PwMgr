using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.ESurface
{
    public class ESurfacePatchProvider
    {
        private GRing2PatchesCreatorProxy _patchesCreator;
        private Ring2PatchStamplingOverseerFinalizer _patchStamper;
        private readonly CommonExecutorUTProxy _commonExecutor;
        private MipmapExtractor _mipmapExtractor;
        private readonly int _mipmapLevelToExtract;

        public ESurfacePatchProvider(GRing2PatchesCreatorProxy patchesCreator, Ring2PatchStamplingOverseerFinalizer patchStamper, CommonExecutorUTProxy commonExecutor,
            MipmapExtractor mipmapExtractor, int mipmapLevelToExtract)
        {
            _patchesCreator = patchesCreator;
            _patchStamper = patchStamper;
            _commonExecutor = commonExecutor;
            _mipmapExtractor = mipmapExtractor;
            _mipmapLevelToExtract = mipmapLevelToExtract;
        }

        public async Task<ESurfaceTexturesPack> ProvideSurfaceDetailAsync(MyRectangle inGamePosition, FlatLod flatLod)
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
            var stampedSlice = await _patchStamper.FinalizeGPatchCreation(onlyPatch, flatLod.ScalarValue);
            await _commonExecutor.AddAction(() => { onlyPatch.Destroy(); });
            if (stampedSlice != null)
            {
                if (_mipmapLevelToExtract != 0)
                {
                    var mipMappedMainTexture = await _mipmapExtractor.ExtractMipmapAsync(new TextureWithSize()
                    {
                        Size = stampedSlice.Resolution,
                        Texture = stampedSlice.ColorStamp
                    }, RenderTextureFormat.ARGB32, _mipmapLevelToExtract);
                    var mipMappedNormalTexture = await _mipmapExtractor.ExtractMipmapAsync(new TextureWithSize()
                    {
                        Size = stampedSlice.Resolution,
                        Texture = stampedSlice.NormalStamp
                    }, RenderTextureFormat.ARGB32, _mipmapLevelToExtract);
                    await _commonExecutor.AddAction(() => {stampedSlice.Destroy(); });

                    return new ESurfaceTexturesPack()
                    {
                        MainTexture = mipMappedMainTexture.Texture,
                        NormalTexture = mipMappedNormalTexture.Texture
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