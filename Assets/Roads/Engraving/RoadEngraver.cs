using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.Spatial;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Roads.Engraving
{
    public class RoadEngraver
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private RoadEngraverConfiguration _configuration;

        public RoadEngraver(ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject, RoadEngraverConfiguration configuration)
        {
            _computeShaderContainer = computeShaderContainer;
            _configuration = configuration;
            _shaderExecutorObject = shaderExecutorObject;
        }

        public async Task<Texture> EngraveRoads(
            MyRectangle terrainCoords,
            IntVector2 textureSize,
            UvdSizedTexture pathProximityTexture,
            Texture heightTexture)
        {
            ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer();

            var shaderHeightTexture = parametersContainer.AddExistingComputeShaderTexture(heightTexture);
            var shaderPathProximityTexture = parametersContainer.AddExistingComputeShaderTexture(
                pathProximityTexture.TextureWithSize.Texture);
            var shderOutHeightTexture = parametersContainer.AddComputeShaderTextureTemplate(
                new MyComputeShaderTextureTemplate()
                {
                    Depth = 24,
                    EnableReadWrite = true,
                    Format = RenderTextureFormat.RFloat,
                    Size = textureSize,
                    TexWrapMode = TextureWrapMode.Clamp
                });

            MultistepComputeShader roadEngravingComputeShader =
                new MultistepComputeShader(_computeShaderContainer.RoadEngravingComputeShader, textureSize);

            var engravingKernel = roadEngravingComputeShader.AddKernel("CSRoad_Engrave");

            roadEngravingComputeShader.SetTexture("HeightTexture",
                shaderHeightTexture, new List<MyKernelHandle>() {engravingKernel});
            roadEngravingComputeShader.SetTexture("PathProximityTexture",
                shaderPathProximityTexture, new List<MyKernelHandle>() {engravingKernel});
            roadEngravingComputeShader.SetTexture("OutHeightTexture",
                shderOutHeightTexture, new List<MyKernelHandle>() {engravingKernel});

            roadEngravingComputeShader.SetGlobalUniform("g_TextureSize", textureSize.ToFloatVec());
            roadEngravingComputeShader.SetGlobalUniform("g_Uvs", pathProximityTexture.Uv.ToVector4());
            roadEngravingComputeShader.SetGlobalUniform("g_GlobalCoords", terrainCoords.ToVector4());
            roadEngravingComputeShader.SetGlobalUniform("g_MaxProximity", _configuration.MaxProximity);
            roadEngravingComputeShader.SetGlobalUniform("g_MaxDelta", _configuration.MaxDelta);
            roadEngravingComputeShader.SetGlobalUniform("g_StartSlopeProximity", _configuration.StartSlopeProximity);
            roadEngravingComputeShader.SetGlobalUniform("g_EndSlopeProximity", _configuration.EndSlopeProximity);

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters(
                new List<MyComputeShaderTextureId>()
                {
                    shderOutHeightTexture
                });

            await _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            {
                OutParameters = outParameters,
                ParametersContainer = parametersContainer,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = roadEngravingComputeShader,
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                KernelHandles = new List<MyKernelHandle>() {engravingKernel},
                                DispatchCount = 1
                            }
                        }
                    }
                }
            });

            return outParameters.RetriveTexture(shderOutHeightTexture);
        }

        public class RoadEngraverConfiguration
        {
            public float MaxProximity = RoadDefaultConstants.MaxProximity;
            public float MaxDelta = RoadDefaultConstants.MaxDelta;
            public float StartSlopeProximity = RoadDefaultConstants.StartSlopeProximity;
            public float EndSlopeProximity = RoadDefaultConstants.EndSlopeProximity;
        }
    }
}