using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ETerrain.ETerrainIntegration;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.FinalExecution;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation.Configurations;
using Assets.Ring2;
using Assets.Ring2.Db;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ESurface
{
    public class ESurfaceDebugGO : MonoBehaviour
    {
        public GameObject Traveller;
        private List<ETerrainHeightPyramidFacade> eTerrainHeightPyramidFacades;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(containerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(400, 400)
                }));
            var meshGeneratorUtProxy = new MeshGeneratorUTProxy(new MeshGeneratorService());


            eTerrainHeightPyramidFacades = new List<ETerrainHeightPyramidFacade>()
            {
                StartTerrainThings(meshGeneratorUtProxy, textureRendererProxy, containerGameObject,
                    new List<HeightPyramidLevel>() {HeightPyramidLevel.Top}),
                //StartTerrainThings(meshGeneratorUtProxy, textureRendererProxy, containerGameObject,
                //    new List<HeightPyramidLevel>() {HeightPyramidLevel.Top, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom})
            };
        }

        private ETerrainHeightPyramidFacade StartTerrainThings(MeshGeneratorUTProxy meshGeneratorUtProxy, UTTextureRendererProxy textureRendererProxy,
            ComputeShaderContainerGameObject containerGameObject, List<HeightPyramidLevel> startConfigurationHeightPyramidLevels)
        {
            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
            //startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>() {HeightPyramidLevel.Top};
            startConfiguration.HeightPyramidLevels = startConfigurationHeightPyramidLevels;

            ETerrainHeightBuffersManager buffersManager = new ETerrainHeightBuffersManager();
            var eTerrainHeightPyramidFacade = new ETerrainHeightPyramidFacade(buffersManager, meshGeneratorUtProxy, textureRendererProxy, startConfiguration);

            var perLevelTemplates = eTerrainHeightPyramidFacade.GenerateLevelTemplates();
            var levels = startConfiguration.HeightPyramidLevels;
            buffersManager.InitializeBuffers(levels.ToDictionary(c => c, c => new EPyramidShaderBuffersGeneratorPerRingInput()
            {
                CeilTextureResolution = startConfiguration.CommonConfiguration.CeilTextureSize.X, //TODO i use only X, - works only for squares
                HeightMergeRanges = perLevelTemplates[c].LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange),
                PyramidLevelWorldSize = startConfiguration.PerLevelConfigurations[c].CeilTextureWorldSize.x, // TODO works only for square pyramids - i use width
                RingUvRanges = startConfiguration.CommonConfiguration.RingsUvRange
            }), startConfiguration.CommonConfiguration.MaxLevelsCount, startConfiguration.CommonConfiguration.MaxRingsPerLevelCount);


            var configuration = new FEConfiguration(new FilePathsConfiguration());
            GlobalServicesProfileInfo servicesProfileInfo = new GlobalServicesProfileInfo();
            var ultraUpdatableContainer = new UltraUpdatableContainer(
                configuration.SchedulerConfiguration,
                servicesProfileInfo, 
                configuration.UpdatableContainerConfiguration);
            var updatableContainer = new UpdatableContainer();
            var intensityPatternPixelsPerUnit = new Dictionary<int, float>()
            {
                {1, 1}
            };
            int mipmapLevelToExtract = 2;
            var plateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                {1, 3}
            };
            var surfacePatchProvider = ESurfaceProviderInitializationHelper.ConstructProvider(
                ultraUpdatableContainer, intensityPatternPixelsPerUnit, containerGameObject, mipmapLevelToExtract, plateStampPixelsPerUnit);
            var surfaceTextureFormat = RenderTextureFormat.ARGB32;

            eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>()
                {
                    //{
                    //    EGroundTextureType.SurfaceTexture, new OneGroundTypeLevelTextureEntitiesGenerator()
                    //    {
                    //        SegmentFillingListenerGeneratorFunc = (level) =>
                    //        {
                    //            var ceilTexture = EGroundTextureGenerator.GenerateEmptyGroundTexture(startConfiguration.CommonConfiguration.CeilTextureSize,
                    //                surfaceTextureFormat);
                    //            var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTexture
                    //                , startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize);
                    //            var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                    //            var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);

                    //            return new SegmentFillingListenerWithCeilTexture()
                    //            {
                    //                CeilTexture = ceilTexture,
                    //                SegmentFillingListener =
                    //                    new LambdaSegmentFillingListener(
                    //                        (c) =>
                    //                        {
                    //                            var segmentLength = startConfiguration.PerLevelConfigurations[level].BiggestShapeObjectInGroupLength;
                    //                            var sap = c.SegmentAlignedPosition;
                    //                            MyRectangle surfaceWorldSpaceRectangle = new MyRectangle(sap.X * segmentLength, sap.Y * segmentLength,
                    //                                segmentLength, segmentLength);
                    //                            var texturesPack = surfacePatchProvider.ProvideSurfaceDetailAsync(surfaceWorldSpaceRectangle, new FlatLod(1, 1)).Result;
                    //                            if (texturesPack != null)
                    //                            {
                    //                                var mainTexture = texturesPack.MainTexture;
                    //                                segmentModificationManager.AddSegmentAsync(mainTexture, c.SegmentAlignedPosition);
                    //                                GameObject.Destroy(mainTexture);
                    //                            }

                    //                            //}
                    //                        },
                    //                        (c) => { },
                    //                        (c) => { })
                    //            };

                    //        },
                    //    }
                    //}
                }
            );

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Bottom);

            return eTerrainHeightPyramidFacade;
        }

        public void Update()
        {
            var position3D = Traveller.transform.position;
            var flatPosition = new Vector2(position3D.x, position3D.z);

            eTerrainHeightPyramidFacades.ForEach(c => c.Update(flatPosition));
        }
    }
}
