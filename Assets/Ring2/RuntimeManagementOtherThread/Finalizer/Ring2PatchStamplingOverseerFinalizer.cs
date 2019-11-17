using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Devising;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.Stamping;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Ring2.RuntimeManagementOtherThread.Finalizer
{
    public class Ring2PatchStamplingOverseerFinalizer : IRing2PatchesOverseerFinalizer
    {
        private UTRing2PlateStamperProxy _stamper;
        private UTTextureRendererProxy _textureRenderer;
        private readonly CommonExecutorUTProxy _commonExecutor;
        private Repositioner _repositioner = Repositioner.Default; //todo

        public Ring2PatchStamplingOverseerFinalizer( UTRing2PlateStamperProxy stamper, UTTextureRendererProxy textureRenderer, CommonExecutorUTProxy commonExecutor) 
        {
            _stamper = stamper;
            _textureRenderer = textureRenderer;
            _commonExecutor = commonExecutor;
        }

        public async Task<List<Ring2PatchDevised>> FinalizePatchesCreation(List<Ring2PatchDevised> devisedPatches) //TODO REMOVE
        {
            var slicesTextures = await TaskUtils.WhenAll(
                devisedPatches.Select(
                    async (c) =>
                        await TaskUtils.WhenAll(c.Plates.Select(
                            p => _stamper.GeneratePlateStampAsync(
                                new Ring2PlateStampTemplate(p.MaterialTemplate,
                                    _repositioner.Move(c.SliceArea)))))));

            List<Ring2PatchDevised> outList = new List<Ring2PatchDevised>();
            for (int i = 0; i < devisedPatches.Count; i++)
            {
                var oldDevisedPatch = devisedPatches[i];
                var textures = slicesTextures[i];

                List<Ring2Plate> platesList = new List<Ring2Plate>();
                for (int plateIdx = 0; plateIdx < oldDevisedPatch.Plates.Count; plateIdx++)
                {
                    var plate = oldDevisedPatch.Plates[plateIdx];
                    var aTexture = textures[plateIdx];
                    MaterialPropertyBlockTemplate propertyBlock = new MaterialPropertyBlockTemplate();
                    propertyBlock.SetTexture("_MainTex", aTexture.ColorStamp);
                    propertyBlock.SetTexture("_NormalTex", aTexture.NormalStamp);

                    var materialTemplate = new MaterialTemplate(Ring2ShaderNames.FromImageTerrainTexture,
                        new ShaderKeywordSet(), propertyBlock);
                    platesList.Add(new Ring2Plate(plate.Mesh, plate.TransformMatrix, materialTemplate));
                }

                var newPatch = new Ring2PatchDevised(platesList, oldDevisedPatch.SliceArea);
                outList.Add(newPatch);
            }
            return outList;
        }

        public async Task<Ring2PlateStamp> FinalizeGPatchCreation(GRing2PatchDevised patch, int flatLod)
        {
            var templates = new List<Ring2PlateStampTemplate>();
            foreach (var sliceInfo in patch.SliceInfos)
            {
                var propertyBlock = sliceInfo.Uniforms.ToPropertyBlockTemplate();

                templates.Add(new Ring2PlateStampTemplate( new MaterialTemplate(
                        Ring2ShaderNames.RuntimeTerrainTexture, sliceInfo.Keywords, propertyBlock), _repositioner.Move(patch.SliceArea), flatLod));
            }

            templates = CreateOnePerKeywordTemplate(templates);

            var slicedTextures = await TaskUtils.WhenAll( templates.Select( (c) => _stamper.GeneratePlateStampAsync(c) ) );

            if (!slicedTextures.Any())
            {
                return null;
            }

            if (slicedTextures.Count == 1 ) 
            {
                return slicedTextures[0];
            }

            var fusedSlices = await FuseSliceStampsAsync(slicedTextures);
            await _commonExecutor.AddAction(() => { slicedTextures.ForEach(c => { c.Destroy(); }); });

            return fusedSlices;
        }

        private List<Ring2PlateStampTemplate> CreateOnePerKeywordTemplate(List<Ring2PlateStampTemplate> templates)
        {
            List<Ring2PlateStampTemplate> outList = new List<Ring2PlateStampTemplate>();
            foreach (var aTemplate in templates)
            {
                var oldMaterialTemplate = aTemplate.MaterialTemplate;
                int layerIndex = 0;
                foreach (var aKeyword in oldMaterialTemplate.KeywordSet.Keywords)
                {
                    Preconditions.Assert(aKeyword.StartsWith("OT_"),
                        "This is hack to use eterrain_Ring2Stampler instead of normal stamper. Each keyword should have start with OT_ and this one starts with " +
                        aKeyword);
                    var propertyBlockTemplate = oldMaterialTemplate.PropertyBlock.Clone();
                    propertyBlockTemplate.SetInt("_LayerIndex", layerIndex);

                    var newTemplate = new MaterialTemplate(oldMaterialTemplate.ShaderName, new ShaderKeywordSet(new List<string>(){aKeyword}), propertyBlockTemplate);
                    outList.Add(new Ring2PlateStampTemplate(newTemplate, aTemplate.PlateCoords, aTemplate.StampLod));
                    layerIndex++;
                }
            }
            return outList;
        }

        private const int MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS_PER_PASS = 5;

        private async Task<Ring2PlateStamp> FuseSliceStampsAsync(List<Ring2PlateStamp> slices)
        {
            Preconditions.Assert(slices.Any(), "There was no slices stamps to create");
            if (slices.Count == 1)
            {
                return slices[0];
            }
            var passCount = Mathf.CeilToInt((float) slices.Count / MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS_PER_PASS);
            if (passCount == 1)
            {
                return await SinglePassFuseSliceStampsAsync(slices);
            }
            else
            {
                var firstPassSlices = slices.Take(MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS_PER_PASS).ToList();
                var secondPassSlices = slices.Skip(MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS_PER_PASS).ToList();
                Preconditions.Assert(firstPassSlices.Count == MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS_PER_PASS, "First pass must have max element count, but it have "+firstPassSlices.Count);

                var firstStamp = await SinglePassFuseSliceStampsAsync(firstPassSlices);
                var secondStamp = await FuseSliceStampsAsync(secondPassSlices);

                var finalStamp = await SinglePassFuseSliceStampsAsync(new List<Ring2PlateStamp>() {firstStamp, secondStamp});// todo more optimal solution. We can merge not 2 but 5

                await _commonExecutor.AddAction(() => firstStamp.Destroy());
                bool shouldDestroySecondStamp = secondPassSlices.Count > 1; //we check if FuseSliceStampsAsync call will create new texture
                if (shouldDestroySecondStamp)
                {
                    await _commonExecutor.AddAction(() => secondStamp.Destroy());
                }

                return finalStamp;
            }
        }

        private async Task<Ring2PlateStamp> SinglePassFuseSliceStampsAsync(List<Ring2PlateStamp> slices)
        {
            Preconditions.Assert(slices.Count>1, $"You should give me at least two slice to merge, but gave {slices.Count}");
            Preconditions.Assert(slices.Count <= MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS_PER_PASS, "Too much elements in slice");

            Preconditions.Assert(slices.Any(), "There was no slices stampes created");
            var size = slices[0].Resolution;

            UniformsPack pack = new UniformsPack();

            for (int i = 0; i < slices.Count; i++)
            {
                pack.SetTexture("_Texture" + i, slices[i].ColorStamp);
            }

            pack.SetUniform("_TexturesCount", slices.Count);
            var fusedColorTexture = await _textureRenderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = true,
                Coords = new MyRectangle(0, 0, 1, 1),
                CreateTexture2D = false,
                OutTextureInfo = new ConventionalTextureInfo(size.X, size.Y, TextureFormat.ARGB32, true),
                RenderTextureFormat = RenderTextureFormat.ARGB32,
                RenderTextureMipMaps = true,
                ShaderName = "Custom/TerrainCreation/SurfaceFuseNonTexArray",
                UniformPack = pack,
            });

            for (int i = 0; i < slices.Count; i++)
            {
                pack.SetTexture("_Texture" + i, slices[i].NormalStamp);
            }

            var fusedNormalTexture = await _textureRenderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                Coords = new MyRectangle(0, 0, 1, 1),
                CreateTexture2D = false,
                OutTextureInfo = new ConventionalTextureInfo(size.X, size.Y, TextureFormat.ARGB32, true),
                RenderTextureFormat = RenderTextureFormat.ARGB32,
                RenderTextureMipMaps = true,
                ShaderName = "Custom/TerrainCreation/SurfaceFuseNonTexArray",
                UniformPack = pack,
            });

            return new Ring2PlateStamp(fusedColorTexture, fusedNormalTexture, size);
        }

        private Texture CreateTextureArary(List<Texture2D> textures, IntVector2 size)
        {
            var colorArray = new Texture2DArray(size.X, size.Y, textures.Count, TextureFormat.ARGB32, false, false);
            for (int i = 0; i < textures.Count; i++)
            {
                var colors = textures[i].GetPixels();
                colorArray.SetPixels(colors, i);
            }
            colorArray.Apply(false);
            colorArray.wrapMode = TextureWrapMode.Clamp;
            return colorArray;
        }
    }
}