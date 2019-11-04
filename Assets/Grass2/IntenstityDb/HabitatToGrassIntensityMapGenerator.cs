using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Grass2.Types;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.Grass2.IntenstityDb
{
    public class HabitatToGrassIntensityMapGenerator
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private CommonExecutorUTProxy _commonExecutor;
        private HabitatToGrassIntensityMapGeneratorConfiguration _configuration;

        public HabitatToGrassIntensityMapGenerator(ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject, CommonExecutorUTProxy commonExecutor,
            HabitatToGrassIntensityMapGeneratorConfiguration configuration)
        {
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
            _configuration = configuration;
            _commonExecutor = commonExecutor;
        }

        public async Task<List<Grass2TypeWithIntensity>> GenerateGrassIntenstiyAsync(
            MyRectangle generationArea,
            Dictionary<HabitatType, Texture2D> habitatTexturesDict,
            IntVector2 habitatTexturesSize,
            UvdSizedTexture pathProximityTexture)
        {
            var generatedGrassTypes = _configuration.GrassTypeToSourceHabitats
                .Where(c => c.Value.Any(k => habitatTexturesDict.ContainsKey(k))).Select(c => c.Key).ToList();

            if (!generatedGrassTypes.Any())
            {
                Debug.Log("W34 Returning empty grass intensity, Because of OutGrassTypes from habitats: " +
                          StringUtils.ToString(habitatTexturesDict.Keys));
                return new List<Grass2TypeWithIntensity>();
            }
////////
            var usedGrassTypesPositions = CreateUsedGrassTypesPositions(generatedGrassTypes);

            var usedHabitatTypesPositions = CreateUsedHabitatTypesPositions(habitatTexturesDict);

            if (!usedHabitatTypesPositions.Any())
            {
                Debug.Log("W34 Returning empty grass intensity, Because of In habitatTypes, from habitats: " +
                          StringUtils.ToString(habitatTexturesDict.Keys));
                return new List<Grass2TypeWithIntensity>();
            }
//////
            var habitatTextureArray = await CreateHabitatTexture2DArrayAsync(habitatTexturesDict, habitatTexturesSize);
//////

            var outFigureSize = new IntVector2(
                Mathf.CeilToInt(generationArea.Width * _configuration.OutputPixelsPerUnit),
                Mathf.CeilToInt(generationArea.Height * _configuration.OutputPixelsPerUnit)
            );

            ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer();

            MultistepComputeShader habitatToGrassShader =
                new MultistepComputeShader(_computeShaderContainer.HabitatToGrassTypeShader, outFigureSize);

            var usedKernelNames = generatedGrassTypes.Select(c => HabitatToGrassUtils.GrassTypeToKernelName[c])
                .ToList();

            var usedKernels = usedKernelNames.Select(c => habitatToGrassShader.AddKernel(c)).ToList();

            var usedGrassTypesPositionsBuffer =
                parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
                {
                    Count = usedGrassTypesPositions.Length,
                    BufferData = usedGrassTypesPositions,
                    Stride = 4,
                    Type = ComputeBufferType.Default
                });
            habitatToGrassShader.SetBuffer("OutputGrassTypePositions", usedGrassTypesPositionsBuffer, usedKernels);

            var usedHabitatTypesPositionsBuffer =
                parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
                {
                    Count = usedHabitatTypesPositions.Length,
                    BufferData = usedHabitatTypesPositions,
                    Stride = 4,
                    Type = ComputeBufferType.Default
                });
            habitatToGrassShader.SetBuffer("InputHabitatTypePositions", usedHabitatTypesPositionsBuffer, usedKernels);

            var outIntensitiesBuffer = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = outFigureSize.X * outFigureSize.Y * generatedGrassTypes.Count,
                Type = ComputeBufferType.Default,
                Stride = 4
            });
            habitatToGrassShader.SetBuffer("OutIntensityBuffer", outIntensitiesBuffer, usedKernels);

            var habitatTextureArrayInShader = parametersContainer.AddExistingComputeShaderTexture(habitatTextureArray);
            habitatToGrassShader.SetTexture("HabitatTexturesArray", habitatTextureArrayInShader, usedKernels);

            var pathProximityTextureInShader =
                parametersContainer.AddExistingComputeShaderTexture(pathProximityTexture.TextureWithSize.Texture);
            habitatToGrassShader.SetTexture("PathProximityTexture", pathProximityTextureInShader, usedKernels);

            habitatToGrassShader.SetGlobalUniform("g_Coords", generationArea.ToVector4());
            habitatToGrassShader.SetGlobalUniform("g_PathProximityUv", pathProximityTexture.Uv.ToVector4());
            habitatToGrassShader.SetGlobalUniform("g_OutTextureSize", outFigureSize.ToFloatVec());
            habitatToGrassShader.SetGlobalUniform("g_MaxProximity", _configuration.MaxProximity);

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters(
                requestedBufferIds: new List<MyComputeBufferId>()
                {
                    outIntensitiesBuffer
                });

            await _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            {
                OutParameters = outParameters,
                ParametersContainer = parametersContainer,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = usedKernels
                            }
                        },
                        Shader = habitatToGrassShader
                    }
                }
            });


            var outIntensitiesNativeArray = await _commonExecutor.AddAction(() =>
            {
                var array = new float[outFigureSize.X * outFigureSize.Y * generatedGrassTypes.Count];
                outParameters.RetriveBuffer(outIntensitiesBuffer).GetData(array);
                //DebugCreateTexture(array, generationArea, outFigureSize, generatedGrassTypes.Count);

                return array;
            });

            return RetriveIntensityFiguresFromTextureArray(outIntensitiesNativeArray, generatedGrassTypes,
                outFigureSize);
        }

        private void DebugCreateTexture(float[] array, MyRectangle generationArea, IntVector2 outFigureSize,
            int count)
        {
            var size = outFigureSize;
            var oneFigSize = outFigureSize.X * outFigureSize.Y;

            for (int i = 0; i < count; i++)
            {
                var newTex = new Texture2D(size.X, size.Y, TextureFormat.ARGB32, false, false);
                for (int y = 0; y < size.Y; y++)
                {
                    for (int x = 0; x < size.X; x++)
                    {
                        var dist = array[y * size.Y + x + oneFigSize * i];
                        newTex.SetPixel(x, y, new Color(dist, 0, 0, 1));
                    }
                }
                var originalName = $"{generationArea}-{DateTime.Now.Ticks}{i}.png";
                originalName = Path.GetInvalidFileNameChars().Aggregate(originalName, (current, c) => current.Replace(c.ToString(), string.Empty));
                SavingFileManager.SaveTextureToPngFile($@"C:\inz\debGrassIntensityTex\"+originalName, newTex);
            }
        }

        private async Task<Texture2DArray> CreateHabitatTexture2DArrayAsync(
            Dictionary<HabitatType, Texture2D> habitatTexturesDict,
            IntVector2 habitatTexturesSize)
        {
            return await _commonExecutor.AddAction(() =>
            {
                //foreach (var keyValuePair in habitatTexturesDict)
                //{
                //    SavingFileManager.SaveTextureToPngFile(
                //        $@"C:\inz\debGrassIntensityTex\{keyValuePair.Key}__{DateTime.Now.Ticks}.png",
                //        keyValuePair.Value);
                //}
                var textureArray = new Texture2DArray(habitatTexturesSize.X, habitatTexturesSize.Y,
                    habitatTexturesDict.Count,
                    TextureFormat.RGB24, //todo change to alpha8?. When changed computeShader sampling allways returns 
                    false, false);
                textureArray.filterMode = FilterMode.Trilinear;
                textureArray.wrapMode = TextureWrapMode.Clamp;
                int r = 0;
                foreach (var habitatTex in habitatTexturesDict.Values)
                {
                    textureArray.SetPixels(habitatTex.GetPixels(), r);
                    r++;
                }
                textureArray.Apply(false);

                return textureArray;
            });
        }

        private static int[] CreateUsedHabitatTypesPositions(Dictionary<HabitatType, Texture2D> habitatTexturesDict)
        {
            var usedHabitatTypesPositions = new int[HabitatToGrassUtils.MaxHabitatTypesCount];
            for (int i = 0; i < HabitatToGrassUtils.MaxGrassTypesCount; i++)
            {
                usedHabitatTypesPositions[i] = HabitatToGrassUtils.InvalidIndex;
            }
            int b = 0;
            foreach (var habitatType in habitatTexturesDict.Keys)
            {
                usedHabitatTypesPositions[HabitatToGrassUtils.RetriveHabitatTypeIndex(habitatType)] = b;
                b++;
            }
            return usedHabitatTypesPositions;
        }

        private static int[] CreateUsedGrassTypesPositions(List<GrassType> generatedGrassTypes)
        {
            var usedGrassTypesPositions = new int[HabitatToGrassUtils.MaxGrassTypesCount];
            for (int i = 0; i < HabitatToGrassUtils.MaxGrassTypesCount; i++)
            {
                usedGrassTypesPositions[i] = HabitatToGrassUtils.InvalidIndex;
            }
            int a = 0;
            foreach (var grassType in generatedGrassTypes)
            {
                usedGrassTypesPositions[HabitatToGrassUtils.RetriveGrassTypeIndex(grassType)] = a;
                a++;
            }
            return usedGrassTypesPositions;
        }

        private List<Grass2TypeWithIntensity> RetriveIntensityFiguresFromTextureArray(
            float[] intensitiesArray,
            List<GrassType> generatedGrassTypes,
            IntVector2 figureSize)
        {
            var outList = new List<Grass2TypeWithIntensity>();
            int i = 0;
            foreach (var type in generatedGrassTypes)
            {
                var intensityFigure = new IntensityFieldFigure(figureSize.X, figureSize.Y);
                for (int y = 0; y < figureSize.Y; y++)
                {
                    for (int x = 0; x < figureSize.X; x++)
                    {
                        intensityFigure.SetPixel(x, y,
                            intensitiesArray[i * (figureSize.X * figureSize.Y) + y * figureSize.X + x]);
                    }
                }
                i++;

                outList.Add(new Grass2TypeWithIntensity()
                {
                    GrassType = type,
                    IntensityFigure = intensityFigure
                });
            }
            return outList;
        }

        public static class HabitatToGrassUtils
        {
            public static int RetriveHabitatTypeIndex(HabitatType type)
            {
                switch (type)
                {
                    case HabitatType.Forest:
                        return 0;
                    case HabitatType.Meadow:
                        return 1;
                    case HabitatType.Scrub:
                        return 2;
                    case HabitatType.Grassland:
                        return 3;
                    case HabitatType.Fell:
                        return 4;
                    case HabitatType.NotSpecified:
                        return 5;
                }
                Preconditions.Fail("unsuppoted habitat type: " + type);
                return -1;
            }

            public static int MaxHabitatTypesCount = 10;

            public static List<HabitatType> InOrderHabitatTypes =
                Enum.GetValues(typeof(HabitatType)).Cast<HabitatType>().OrderBy(c => RetriveHabitatTypeIndex(c))
                    .ToList();

            public static int RetriveGrassTypeIndex(GrassType type)
            {
                switch (type)
                {
                    case GrassType.Debug1:
                        return 0;
                    case GrassType.Debug2:
                        return 1;
                }
                Preconditions.Fail("unsupported grass type " + type);
                return -1;
            }

            public static List<GrassType> InOrderGrassTypes =
                Enum.GetValues(typeof(GrassType)).Cast<GrassType>().OrderBy(c => RetriveGrassTypeIndex(c))
                    .ToList();

            public static int MaxGrassTypesCount = 10;

            public static int InvalidIndex = 50;

            public static Dictionary<GrassType, string> GrassTypeToKernelName = new Dictionary<GrassType, string>()
            {
                {GrassType.Debug1, "CSHabitatToGrassType_Debug1"},
                {GrassType.Debug2, "CSHabitatToGrassType_Debug2"},
            };
        }

        public class HabitatToGrassIntensityMapGeneratorConfiguration
        {
            public float OutputPixelsPerUnit;
            public Dictionary<GrassType, List<HabitatType>> GrassTypeToSourceHabitats;
            public float MaxProximity = 5;
        }
    }
}