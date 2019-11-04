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
        private Repositioner _repositioner = Repositioner.Default; //todo

        public Ring2PatchStamplingOverseerFinalizer(
            UTRing2PlateStamperProxy stamper,
            UTTextureRendererProxy textureRenderer)
            
        {
            _stamper = stamper;
            _textureRenderer = textureRenderer;
        }

        public async Task<List<Ring2PatchDevised>> FinalizePatchesCreation(List<Ring2PatchDevised> devisedPatches)
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

            var slicedTextures = await TaskUtils.WhenAll(
                templates.Select(
                    (c) => _stamper.GeneratePlateStampAsync(c)
                )
            );

            if (!slicedTextures.Any())
            {
                return null;
            }

            int MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS = 5;
            Preconditions.Assert(slicedTextures.Count <= MAX_SUPPORTED_TEXTURE_ARRAY_ELEMENTS,
                "Too much elements in slice");
            if (slicedTextures.Count == 1 ) //todo repair
            {
                return slicedTextures[0];
            }

            var fusedSlices = await FuseSliceStampsAsync(slicedTextures);

            //_commonExecutor.AddAction(() =>
            //{
            //    foreach (var stamp in slicedTextures)
            //    {
            //        GameObject.Destroy(stamp.ColorStamp);
            //        GameObject.Destroy(stamp.NormalStamp);
            //    }
            //});
            return fusedSlices;
        }

        private async Task<Ring2PlateStamp> FuseSliceStampsAsync(List<Ring2PlateStamp> slices)
        {
            Preconditions.Assert(slices.Any(), "There was no slices stampes created");
            var size = slices[0].Resolution;

            UniformsPack pack = new UniformsPack();

            //Ring2PlateStamp arrayedStamp = await _commonExecutor.AddAction(() =>
            //{
            //    return new Ring2PlateStamp(
            //        CreateTextureArary(slices.Select(c => c.ColorStamp).Cast<Texture2D>().ToList(), size),
            //        CreateTextureArary(slices.Select(c => c.NormalStamp).Cast<Texture2D>().ToList(), size),
            //        size
            //    );
            //});

            for (int i = 0; i < slices.Count; i++)
            {
                pack.SetTexture("_Texture" + i, slices[i].ColorStamp);
            }

            //pack.SetTexture("_TexturesArray", arrayedStamp.ColorStamp);
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