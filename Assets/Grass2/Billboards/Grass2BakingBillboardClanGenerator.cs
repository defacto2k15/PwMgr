using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Grass2.Billboards
{
    public class Grass2BakingBillboardClanGenerator
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public Grass2BakingBillboardClanGenerator(
            ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
        }

        public async Task<Grass2BakedBillboardClan> GenerateBakedAsync(Grass2SingledBillboardClan clan)
        {
            return await GenerateBakedBillboardTextures(clan.BillboardsList);
        }

        private async Task<Grass2BakedBillboardClan> GenerateBakedBillboardTextures(
            List<DetailedGrass2SingleBillboard> singleBillboards)
        {
            var textureSize = new IntVector2(singleBillboards.First().Texture.width,
                singleBillboards.First().Texture.height);
            var inputBillboardsTextureArray = new Texture2DArray(textureSize.X, textureSize.Y, singleBillboards.Count,
                TextureFormat.ARGB32, false, false);

            for (int i = 0; i < singleBillboards.Count; i++)
            {
                var tex = singleBillboards[i].Texture;
                inputBillboardsTextureArray.SetPixels(tex.GetPixels(), i);
            }
            inputBillboardsTextureArray.Apply(false);

            var parametersContainer = new ComputeShaderParametersContainer();
            var bladeSeedTextureArray = parametersContainer.AddComputeShaderTextureTemplate(
                new MyComputeShaderTextureTemplate()
                {
                    Depth = 0,
                    EnableReadWrite = true,
                    Format = RenderTextureFormat.R8,
                    Size = textureSize,
                    TexWrapMode = TextureWrapMode.Clamp,
                    Dimension = TextureDimension.Tex2DArray,
                    VolumeDepth = singleBillboards.Count
                });

            var detailTextureArray = parametersContainer.AddComputeShaderTextureTemplate(
                new MyComputeShaderTextureTemplate()
                {
                    Depth = 0,
                    EnableReadWrite = true,
                    Format = RenderTextureFormat.RG16,
                    Size = textureSize,
                    TexWrapMode = TextureWrapMode.Clamp,
                    Dimension = TextureDimension.Tex2DArray,
                    VolumeDepth = singleBillboards.Count
                });


            MultistepComputeShader singleToDuoGrassBillboardShader =
                new MultistepComputeShader(_computeShaderContainer.SingleToDuoBillboardShader, textureSize);
            singleToDuoGrassBillboardShader.SetGlobalUniform("g_ArrayLength", singleBillboards.Count);

            var transferKernel = singleToDuoGrassBillboardShader.AddKernel("CSSingleToDuoBillboard_Transfer");

            var inputSingleTextureArray =
                parametersContainer.AddExistingComputeShaderTexture(inputBillboardsTextureArray);
            singleToDuoGrassBillboardShader.SetTexture("InputSingleTextureArray", inputSingleTextureArray,
                new List<MyKernelHandle>() {transferKernel});

            singleToDuoGrassBillboardShader.SetTexture("OutputBladeSeedTextureArray", bladeSeedTextureArray,
                new List<MyKernelHandle>() {transferKernel});
            singleToDuoGrassBillboardShader.SetTexture("OutputDetailTextureArray", detailTextureArray,
                new List<MyKernelHandle>() {transferKernel});

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters(
                new List<MyComputeShaderTextureId>()
                {
                    bladeSeedTextureArray,
                    detailTextureArray
                });
            await _shaderExecutorObject.AddOrder(new ComputeShaderOrder()
            {
                ParametersContainer = parametersContainer,
                OutParameters = outParameters,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = singleToDuoGrassBillboardShader,
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = new List<MyKernelHandle>() {transferKernel}
                            }
                        }
                    },
                }
            });

            var outBladeSeedTexture = outParameters.RetriveTexture(bladeSeedTextureArray);
            outBladeSeedTexture.filterMode = FilterMode.Point;
            outBladeSeedTexture.wrapMode = TextureWrapMode.Clamp;

            var outDetailTexture = outParameters.RetriveTexture(detailTextureArray);
            outDetailTexture.filterMode = FilterMode.Trilinear;
            outDetailTexture.wrapMode = TextureWrapMode.Clamp;

            return new Grass2BakedBillboardClan(singleBillboards.Select(c => c.BladesCount).ToList(),
                outBladeSeedTexture, outDetailTexture);
        }
    }
}