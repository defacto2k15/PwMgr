using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Random;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class MTTerrainDetailProviderDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;

        private TerrainDetailFileManager _terrainDetailFileManager;
        private TerrainDetailProviderConfiguration _terrainDetailProviderConfiguration;
        private UTTextureRendererProxy _utTextureRendererProxy;

        private GenericAsyncExecutionThreadProxy _executionThreadProxy;
        private ConcurrentBag<MyTexturePair> _resultBag = new ConcurrentBag<MyTexturePair>();
        private CommonExecutorUTProxy _commonExecutor = new CommonExecutorUTProxy();
        private RandomProvider _randomProvider = new RandomProvider(123);
        private UnityThreadComputeShaderExecutorObject _unityThreadComputeShaderExecutorObject;

        private class MyTexturePair
        {
            public Texture Tex0;
            public Texture Tex1;
        }


        public void Start()
        {
            _unityThreadComputeShaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            var globalHeightTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\n49_e019_1arc_v3.png",
                3600, 3600,
                TextureFormat.ARGB32, true, false);
            var sizedGlobalHeightTexture = new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = globalHeightTexture
            };

            TaskUtils.SetGlobalMultithreading(true);

            _executionThreadProxy = new GenericAsyncExecutionThreadProxy("TerrainProviding");
            _executionThreadProxy.StartThreading(() => { });
            _executionThreadProxy.PostAction(async () =>
            {
                InitializeFields();
                //var tex0 = await CreateGenerateAndGenerateTexture(new List<RankedTerrainFeatureApplier>()
                //{
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 1,
                //        Applier = new RandomNoiseTerrainFeatureApplier(_utTextureRendererProxy, _commonExecutor,
                //            new Dictionary<TerrainCardinalResolution, RandomNoiseTerrainFeatureApplierConfiguration>
                //        {
                //            { TerrainCardinalResolution.LOW_RESOLUTION, new RandomNoiseTerrainFeatureApplierConfiguration() { DetailResolutionMultiplier = 1} },
                //            { TerrainCardinalResolution.MID_RESOLUTION, new RandomNoiseTerrainFeatureApplierConfiguration() {DetailResolutionMultiplier =  8}},
                //        }),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 2,
                //        Applier = new DiamondSquareTerrainFeatureApplier(
                //            new DiamondSquareCreator(_randomProvider), _commonExecutor,
                //            _utTextureRendererProxy),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 3,
                //        Applier = new ThermalErosionTerrainFeatureApplier(ContainerGameObject,
                //            _unityThreadComputeShaderExecutorObject, _commonExecutor),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 4,
                //        Applier = new HydraulicErosionTerrainFeatureApplier(ContainerGameObject,_unityThreadComputeShaderExecutorObject),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    //new RankedTerrainFeatureApplier()
                //    //{
                //    //    Rank = 6,
                //    //    Applier = new TweakedThermalErosionTerrainFeatureApplier(ContainerGameObject, _unityThreadComputeShaderExecutorObject),
                //    //    AvalibleResolutions = new List<TerrainCardinalResolution>()
                //    //    {
                //    //        TerrainCardinalResolution.LOW_RESOLUTION,
                //    //        TerrainCardinalResolution.MID_RESOLUTION,
                //    //        TerrainCardinalResolution.MAX_RESOLUTION
                //    //    }
                //    //}

                //}, sizedGlobalHeightTexture);

                //var tex1 = await CreateGenerateAndGenerateTexture(new List<RankedTerrainFeatureApplier>()
                //{
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 1,
                //        Applier = new RandomNoiseTerrainFeatureApplier(_utTextureRendererProxy, _commonExecutor,
                //            new Dictionary<TerrainCardinalResolution, RandomNoiseTerrainFeatureApplierConfiguration>
                //        {
                //            { TerrainCardinalResolution.LOW_RESOLUTION, new RandomNoiseTerrainFeatureApplierConfiguration() { DetailResolutionMultiplier = 1} },
                //            { TerrainCardinalResolution.MID_RESOLUTION, new RandomNoiseTerrainFeatureApplierConfiguration() {DetailResolutionMultiplier =  8}},
                //        }),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 2,
                //        Applier = new DiamondSquareTerrainFeatureApplier(
                //            new DiamondSquareCreator(_randomProvider), _commonExecutor,
                //            _utTextureRendererProxy),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 3,
                //        Applier = new ThermalErosionTerrainFeatureApplier(ContainerGameObject,
                //            _unityThreadComputeShaderExecutorObject, _commonExecutor),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    new RankedTerrainFeatureApplier()
                //    {
                //        Rank = 6,
                //        Applier = new HydraulicErosionTerrainFeatureApplier(ContainerGameObject,_unityThreadComputeShaderExecutorObject),
                //        AvalibleResolutions = new List<TerrainCardinalResolution>()
                //        {
                //            TerrainCardinalResolution.LOW_RESOLUTION,
                //            TerrainCardinalResolution.MID_RESOLUTION,
                //            TerrainCardinalResolution.MAX_RESOLUTION
                //        }
                //    },
                //    //new RankedTerrainFeatureApplier()
                //    //{
                //    //    Rank = 5,
                //    //    Applier = new TweakedThermalErosionTerrainFeatureApplier(ContainerGameObject, _unityThreadComputeShaderExecutorObject),
                //    //    AvalibleResolutions = new List<TerrainCardinalResolution>()
                //    //    {
                //    //        TerrainCardinalResolution.MID_RESOLUTION,
                //    //        TerrainCardinalResolution.MAX_RESOLUTION
                //    //    }
                //    //}

                //}, sizedGlobalHeightTexture);


                //_resultBag.Add(new MyTexturePair()
                //{
                //    Tex0 = tex0,
                //    Tex1 = tex1
                //});
            });
        }

        public void Update()
        {
            _utTextureRendererProxy.Update();
            _commonExecutor.Update();
            _unityThreadComputeShaderExecutorObject.Update();
            MyTexturePair texturePair;
            bool takeSucceded = _resultBag.TryTake(out texturePair);
            if (takeSucceded)
            {
                var unitySideLength = 10f;
                var realSideLength = 240 * 24;
                var metersPerUnit = realSideLength / unitySideLength;

                var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var material = new Material(Shader.Find("Custom/Terrain/Terrain_Debug"));
                gameObject.GetComponent<MeshRenderer>().material = material;
                gameObject.name = "Terrain";
                gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
                gameObject.transform.localScale = new Vector3(10, 6500 / metersPerUnit, 10);
                gameObject.transform.localPosition = new Vector3(0, 0, 0);
                gameObject.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);

                material.SetTexture("_HeightmapTex0", texturePair.Tex0);
                material.SetTexture("_HeightmapTex1", texturePair.Tex1);
            }
        }

        private void InitializeFields()
        {
            _terrainDetailProviderConfiguration = new TerrainDetailProviderConfiguration()
            {
                UseTextureSavingToDisk = false
            };
            _terrainDetailFileManager = new TerrainDetailFileManager("C:\\unityCache\\", new CommonExecutorUTProxy());

            TextureRendererServiceConfiguration rendererServiceConfiguration = new TextureRendererServiceConfiguration()
            {
                StepSize = new Vector2(500, 500)
            };
            _utTextureRendererProxy = new UTTextureRendererProxy(
                new TextureRendererService(new MultistepTextureRenderer(ContainerGameObject),
                    rendererServiceConfiguration));
        }

        private TerrainDetailProvider CreateTerrainDetailProvider(List<RankedTerrainFeatureApplier> featureAppliers,
            TextureWithSize globalHeightTexture)
        {
            TerrainDetailGeneratorConfiguration generatorConfiguration = new TerrainDetailGeneratorConfiguration()
            {
                TerrainDetailImageSideDisjointResolution = 240
            };
            TextureWithCoords fullFundationData = new TextureWithCoords(sizedTexture: globalHeightTexture,
                coords: new MyRectangle(0, 0, 3601 * 24, 3601 * 24));

            TerrainDetailGenerator generator =
                new TerrainDetailGenerator(generatorConfiguration, _utTextureRendererProxy, fullFundationData,
                    featureAppliers, _commonExecutor);

            TerrainDetailProvider provider =
                new TerrainDetailProvider(_terrainDetailProviderConfiguration, generator, null, new TerrainDetailAlignmentCalculator(240));
            return provider;
        }

        private async Task<Texture> CreateGenerateAndGenerateTexture(List<RankedTerrainFeatureApplier> featureAppliers,
            TextureWithSize globalHeightTexture)
        {
            var provider = CreateTerrainDetailProvider(featureAppliers, globalHeightTexture);

            MyRectangle queryArea = new MyRectangle(0, 0, 24 * 240, 24 * 240);
            var resolution = TerrainCardinalResolution.MIN_RESOLUTION;

            var outTex = (await provider.GenerateHeightDetailElementAsync(queryArea, resolution, CornersMergeStatus.NOT_MERGED)).Texture;
            return outTex.Texture;
        }
    }
}