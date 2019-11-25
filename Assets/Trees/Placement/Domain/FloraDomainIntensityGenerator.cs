using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using UnityEngine;

namespace Assets.Trees.Placement.Domain
{
    public class FloraDomainIntensityGenerator : IStoredPartsGenerator<FloraDomainIntensityArea>
    {
        private List<FloraDomainCreationTemplate> _creationTemplates;
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private CommonExecutorUTProxy _commonExecutor;
        private float _seed;
        private FloraDomainIntensityGeneratorConfiguration _configuration;

        public FloraDomainIntensityGenerator(
            List<FloraDomainCreationTemplate> creationTemplates,
            ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject,
            CommonExecutorUTProxy commonExecutor,
            float seed,
            FloraDomainIntensityGeneratorConfiguration configuration)
        {
            _creationTemplates = creationTemplates;
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
            _commonExecutor = commonExecutor;
            _seed = seed;
            _configuration = configuration;
        }

        public async Task<CoordedPart<FloraDomainIntensityArea>> GeneratePartAsync(MyRectangle queryArea)
        {
            var textureSize = new IntVector2(
                Mathf.CeilToInt(queryArea.Width * _configuration.PixelsPerUnit),
                Mathf.CeilToInt(queryArea.Height * _configuration.PixelsPerUnit));

            var domainCharacteristicsArray = _creationTemplates.Select(c => new FloraDomainGenerationCharacteristic()
            {
                PositionMultiplier = c.PositionMultiplier,
                MaxIntensity = c.MaxIntensity,
                MinIntensity = c.MinIntensity
            }).ToArray();


            var parametersContainer = new ComputeShaderParametersContainer();

            var outIntensityArray = parametersContainer.AddComputeBufferTemplate(
                new MyComputeBufferTemplate()
                {
                    Count = textureSize.X * textureSize.Y * domainCharacteristicsArray.Length,
                    Stride = 4,
                    Type = ComputeBufferType.Default
                });

            var shaderDomainCharacteristicsArray = parametersContainer.AddComputeBufferTemplate(
                new MyComputeBufferTemplate()
                {
                    BufferData = domainCharacteristicsArray,
                    Count = domainCharacteristicsArray.Length,
                    Stride = 4 * 3,
                    Type = ComputeBufferType.Default
                });


            MultistepComputeShader singleToDuoGrassBillboardShader =
                new MultistepComputeShader(_computeShaderContainer.FloraDomainShader, textureSize);
            singleToDuoGrassBillboardShader.SetGlobalUniform("g_ArrayLength", textureSize.X);
            singleToDuoGrassBillboardShader.SetGlobalUniform("g_DomainTypesCount", domainCharacteristicsArray.Length);
            singleToDuoGrassBillboardShader.SetGlobalUniform("g_Coords", queryArea.ToVector4());
            singleToDuoGrassBillboardShader.SetGlobalUniform("g_Seed", _seed);

            var transferKernel = singleToDuoGrassBillboardShader.AddKernel("CSFloraDomain_Calculate");

            singleToDuoGrassBillboardShader.SetBuffer("DomainCharacteristicsBuffer", shaderDomainCharacteristicsArray,
                new List<MyKernelHandle>()
                {
                    transferKernel
                });

            singleToDuoGrassBillboardShader.SetBuffer("OutputIntensityArray", outIntensityArray,
                new List<MyKernelHandle>() {transferKernel});

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters(
                requestedBufferIds: new List<MyComputeBufferId>()
                {
                    outIntensityArray
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

            Dictionary<FloraDomainType, IntensityFieldFigure> intensityDict =
                await _commonExecutor.AddAction(() =>
                {
                    var singleLayerLength = textureSize.X * textureSize.Y;
                    var retrivedBuffer = new float[singleLayerLength * _creationTemplates.Count];
                    outParameters.RetriveBuffer(outIntensityArray).GetData(retrivedBuffer);

                    var dict = new Dictionary<FloraDomainType, IntensityFieldFigure>();
                    for (int i = 0; i < _creationTemplates.Count; i++)
                    {
                        var type = _creationTemplates[i].Type;
                        var figure = new IntensityFieldFigure(textureSize.X, textureSize.Y);


                        for (int x = 0; x < textureSize.X; x++)
                        {
                            for (int y = 0; y < textureSize.Y; y++)
                            {
                                figure.SetPixel(x, y, retrivedBuffer[singleLayerLength * i + y * textureSize.X + x]);
                            }
                        }
                        //CreateDebugTexture(figure,type, queryArea);
                        dict[type] = figure;
                    }
                    return dict;
                });

            var outIntensityArea = new FloraDomainIntensityArea(intensityDict);
            return new CoordedPart<FloraDomainIntensityArea>()
            {
                Part = outIntensityArea,
                Coords = queryArea
            };
        }


        private static int texNo = 0;

        private void CreateDebugTexture(IntensityFieldFigure figure, FloraDomainType type,
            MyRectangle queryArea)
        {
            var size = new IntVector2(figure.Height, figure.Width);
            var tex = new Texture2D(size.X, size.Y, TextureFormat.ARGB32, false, false);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    tex.SetPixel(x, y, new Color(figure.GetPixel(x, y), 0, 0));
                }
            }
            tex.Apply();
            SavingFileManager.SaveTextureToPngFile(
                $@"C:\inz2\unityScreenshots\biome-{texNo++}-{type}-{queryArea.X}_{queryArea.Y}_{queryArea.Width}_{
                        queryArea.Height
                    }.png", tex);
        }

        public struct FloraDomainGenerationCharacteristic
        {
            public float MinIntensity;
            public float MaxIntensity;
            public float PositionMultiplier;
        }
    }

    public class FloraDomainCreationTemplate
    {
        public FloraDomainType Type;
        public float MinIntensity;
        public float MaxIntensity;
        public float PositionMultiplier;
    }

    public class FloraDomainIntensityGeneratorConfiguration
    {
        public float PixelsPerUnit;
    }
}