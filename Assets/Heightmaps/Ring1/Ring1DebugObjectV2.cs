using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Caching;
using Assets.ComputeShaders;
using Assets.FinalExecution;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.GRing.DynamicLod;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.MT;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Ring2.Stamping;
using Assets.TerrainMat;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1DebugObjectV2 : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;
        public Camera ActiveCamera;

        private UpdatableContainer _updatableContainer;
        private List<OtherThreadProxyAndActionPair> _otherThreadActionPairs = new List<OtherThreadProxyAndActionPair>();
        private Ring1Tree _ring1Tree;
        private Ring1TreeProxy _ring1TreeProxy;
        private StainTerrainResourceCreatorUTProxy _stainTerrainResourceCreatorUtProxy;

        private Ring2PatchesPainterUTProxy _ring2PatchesPainterUtProxy;

        // Use this for initialization
        void Start()
        {
            _updatableContainer = new UpdatableContainer();
            TaskUtils.SetGlobalMultithreading(false);

            _ring1Tree = new Ring1Tree();

            //////////////////

            var rgbaMainTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\n49_e019_1arc_v3.png", 3600,
                3600,
                TextureFormat.ARGB32, true, false);


            CommonExecutorUTProxy commonExecutorUtProxy = new CommonExecutorUTProxy(); //todo
            _updatableContainer.AddUpdatableElement(commonExecutorUtProxy);
            TerrainTextureFormatTransformator transformator =
                new TerrainTextureFormatTransformator(commonExecutorUtProxy);
            var globalHeightTexture = transformator.EncodedHeightTextureToPlain(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = rgbaMainTexture
            });

            /// /// VISIBILITY TEXTURE
            var visibilityTextureSideLength = 16;
            var visibilityTexture = new Texture2D(visibilityTextureSideLength, visibilityTextureSideLength,
                TextureFormat.RFloat, false);
            visibilityTexture.filterMode = FilterMode.Point;

            var visibilityTextureProcessorProxy =
                new Ring1VisibilityTextureProcessorUTProxy(new Ring1VisibilityTextureProcessor(visibilityTexture));
            _updatableContainer.AddUpdatableElement(visibilityTextureProcessorProxy);


            var visibilityTextureChangeGrabber = new Ring1VisibilityTextureChangeGrabber();

            var terrainParentGameObject = new GameObject("TerrainParent");

            var unityCoordsCalculator = new UnityCoordsCalculator(new Vector2(24 * 240 * 2, 24 * 240 * 2));
            var orderGrabber = new Ring1PaintingOrderGrabber();

            var painterProxy = new RingTerrainPainterUTProxy(new RingTerrainPainter());
            _updatableContainer.AddUpdatableElement(painterProxy);

            painterProxy.Update();

            var mainRespondingProxy = new Ring1NodeEventMainRespondingProxy(new Ring1NodeEventMainResponder());
            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = mainRespondingProxy,
                EveryPostAction =
                    () =>
                    {
                        var delta = visibilityTextureChangeGrabber.RetriveVisibilityChanges();

                        if (delta.AnyChange)
                        {
                            var visibilityTextureChagnes = visibilityTextureChangeGrabber.RetriveVisibilityChanges();
                            visibilityTextureProcessorProxy.AddOrder(visibilityTextureChagnes);
                        }

                        if (orderGrabber.IsAnyOrder)
                        {
                            painterProxy.AddOrder(orderGrabber.RetriveOrderAndClear());
                        }
                    }
            });


            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(ContainerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(500, 500)
                }));
            _updatableContainer.AddUpdatableElement(textureRendererProxy);

            UnityThreadComputeShaderExecutorObject computeShaderExecutorObject =
                new UnityThreadComputeShaderExecutorObject();
            _updatableContainer.AddUpdatableElement(computeShaderExecutorObject);
            _updatableContainer.AddUpdatableElement(commonExecutorUtProxy);

            TerrainDetailGenerator terrainDetailGenerator =
                CreateTerrainDetailGenerator(
                    globalHeightTexture, textureRendererProxy, commonExecutorUtProxy, computeShaderExecutorObject,
                    ContainerGameObject);
            TerrainDetailProvider terrainDetailProvider =
                CreateTerrainDetailProvider(terrainDetailGenerator, commonExecutorUtProxy);

            var terrainShapeDb = FETerrainShapeDbInitialization.CreateTerrainShapeDb(terrainDetailProvider, commonExecutorUtProxy, new TerrainDetailAlignmentCalculator(240));
            TerrainShapeDbProxy terrainShapeDbProxy = new TerrainShapeDbProxy(terrainShapeDb);
            terrainDetailGenerator.SetBaseTerrainDetailProvider(BaseTerrainDetailProvider.CreateFrom(terrainShapeDb));

            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = terrainShapeDbProxy
            });

            var meshGeneratorProxy = new MeshGeneratorUTProxy(new MeshGeneratorService());
            _updatableContainer.AddUpdatableElement(meshGeneratorProxy);

            _stainTerrainResourceCreatorUtProxy =
                new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator());
            _updatableContainer.AddUpdatableElement(_stainTerrainResourceCreatorUtProxy);

            var stainTerrainServiceProxy = new StainTerrainServiceProxy(
                new StainTerrainService(
                    new ComputationStainTerrainResourceGenerator(
                        new StainTerrainResourceComposer(
                            _stainTerrainResourceCreatorUtProxy
                        ),
                        new StainTerrainArrayMelder(),
                        new DummyStainTerrainArrayFromBiomesGenerator(
                            new DebugBiomeContainerGenerator().GenerateBiomesContainer(
                                new BiomesContainerConfiguration()),
                            new StainTerrainArrayFromBiomesGeneratorConfiguration()
                        )),
                    new MyRectangle(0, 0, 24 * 240 * 2, 24 * 240 * 2)));
            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = stainTerrainServiceProxy
            });

            var gRing1NodeTerrainCreator = new GRing1NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                meshGeneratorProxy,
                terrainShapeDbProxy,
                stainTerrainServiceProxy,
                unityCoordsCalculator,
                null,
                null,
                new GRingGroundShapeProviderConfiguration(),
                new GRingTerrainMeshProviderConfiguration());

            var gRing2NodeTerrainCreator = new GRing2NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                meshGeneratorProxy,
                terrainShapeDbProxy,
                unityCoordsCalculator,
                new GRing2PatchesCreatorProxy(CreateRing2PatchesCreator()),
                null,
                null,
                new GRingGroundShapeProviderConfiguration(),
                new GRingTerrainMeshProviderConfiguration());

            UTRing2PlateStamperProxy stamperProxy = new UTRing2PlateStamperProxy(
                new Ring2PlateStamper(new Ring2PlateStamperConfiguration()
                {
                    PlateStampPixelsPerUnit = new Dictionary<int, float>()
                }, ContainerGameObject));
            _updatableContainer.AddUpdatableElement(stamperProxy);

            Ring2PatchStamplingOverseerFinalizer patchStamper =
                new Ring2PatchStamplingOverseerFinalizer(stamperProxy, textureRendererProxy);

            var gStampedRing2NodeTerrainCreator = new GStampedRing2NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                meshGeneratorProxy,
                terrainShapeDbProxy,
                unityCoordsCalculator,
                new GRing2PatchesCreatorProxy(CreateRing2PatchesCreator()),
                patchStamper,
                null,
                null,
                new GRingGroundShapeProviderConfiguration(),
                new GRingTerrainMeshProviderConfiguration());

            var subCreator = new SupremeGRingNodeTerrainCreator(new List<NewListenersCreatorWithLimitation>()
            {
                //new NewListenersCreatorWithMaximumLod()
                //{
                //    Creator = gRing1NodeTerrainCreator,
                //    MaximumLod = new FlatLod(6)
                //},
                new NewListenersCreatorWithLimitation()
                {
                    Creator = new GVoidNodeTerrainCreator(),
                    MaximumLod = new FlatLod(6)
                },
                //new NewListenersCreatorWithMaximumLod()
                //{
                //    Creator = gRing2NodeTerrainCreator,
                //    MaximumLod = new FlatLod(8)
                //}
                new NewListenersCreatorWithLimitation()
                {
                    Creator = gStampedRing2NodeTerrainCreator,
                    MaximumLod = new FlatLod(9)
                }
            });

            var eventCollector = new Ring1NodeEventCollector(
                new DynamicFlatLodGRingNodeTerrainCreator(subCreator, new FlatLodCalculator(unityCoordsCalculator)));

            _ring1TreeProxy = new Ring1TreeProxy(_ring1Tree);
            _otherThreadActionPairs.Add(new OtherThreadProxyAndActionPair()
            {
                Proxy = _ring1TreeProxy,
                EveryPostAction =
                    () =>
                    {
                        if (eventCollector.Any)
                        {
                            mainRespondingProxy.AddOrder(eventCollector.RetriveOrderAndClear());
                        }
                    }
            });

            var baseQuadSideLength = 90f;

            StartThreading(_otherThreadActionPairs);
            _ring1TreeProxy.CreateHeightmap(new Ring1Tree.RootNodeCreationParameters()
            {
                UnityCoordsCalculator = unityCoordsCalculator,
                NodeListener = eventCollector,
                PrecisionDistances =
                    new Dictionary<float, int>
                    {
                        //{4f * 50f/3f, 9},
                        //{4f * 50f/2f, 8},
                        {CalculatePrecisionDistance(baseQuadSideLength, 2, 1), 7},
                        {6.5f * 50f, 6},
                        {20 * 50f, 5},
                        {40 * 50f, 4},
                        {50 * 50f, 3},
                        {100 * 50f, 2},
                        {200 * 50f, 1}
                    },
                InitialCameraPosition = Vector3.zero,
            });
        }

        private float CalculatePrecisionDistance(float baseQuadSideLength, int xMove, int yMove)
        {
            return (new Vector2(xMove - 0.5f, yMove - 0.5f) * baseQuadSideLength).magnitude * 1.02f;
        }

        private GRing2PatchesCreator CreateRing2PatchesCreator()
        {
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            TextureConcieverUTProxy conciever = new TextureConcieverUTProxy();
            _updatableContainer.AddUpdatableElement(conciever);

            _ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(
                new Ring2PatchesPainter(
                    new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));
            _updatableContainer.AddUpdatableElement(_ring2PatchesPainterUtProxy);

            Ring2RandomFieldFigureGenerator figureGenerator = new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(2, 2)
                });
            var utFigureGenerator = new RandomFieldFigureGeneratorUTProxy(figureGenerator);
            _updatableContainer.AddUpdatableElement(utFigureGenerator);

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(utFigureGenerator,
                new Ring2RandomFieldFigureRepositoryConfiguration(2, new Vector2(20, 20)));

            Quadtree<Ring2Region> regionsTree = Ring2TestUtils.CreateRegionsTreeWithPath3(randomFieldFigureRepository);

            return new GRing2PatchesCreator(
                new Ring2RegionsDatabase(regionsTree),
                new GRing2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever),
                new GRing2Deviser(),
                new Ring2PatchesOverseerConfiguration()
                {
                    IntensityPatternPixelsPerUnit = new Dictionary<int, float>()
                    {
                        {1, 1}
                    }
                    //PatchSize = new Vector2(90, 90)
                }
            );
        }

        public static void StartThreading(List<OtherThreadProxyAndActionPair> otherThreadActionPairs)
        {
            if (TaskUtils.GetGlobalMultithreading())
            {
                foreach (var pair in otherThreadActionPairs)
                {
                    pair.Proxy.StartThreading(pair.EveryPostAction);
                }
            }
        }

        public void Update()
        {
            _updatableContainer.Update();
            if (!TaskUtils.GetGlobalMultithreading())
            {
                foreach (var pair in _otherThreadActionPairs)
                {
                    pair.EveryPostAction?.Invoke();
                }
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                UpdateCameraPosition();
            }
        }

        public void UpdateCameraPosition()
        {
            _ring1TreeProxy.UpdateCamera(FovData.FromCamera(ActiveCamera));
        }

        public static TerrainDetailGenerator CreateTerrainDetailGenerator(
            Texture mainTexture, UTTextureRendererProxy utTextureRendererProxy,
            CommonExecutorUTProxy commonExecutorUtProxy,
            UnityThreadComputeShaderExecutorObject computeShaderExecutorObject,
            ComputeShaderContainerGameObject containerGameObject)
        {
            var featureAppliers =
                TerrainDetailProviderDebugUtils.CreateFeatureAppliers(utTextureRendererProxy, containerGameObject,
                    commonExecutorUtProxy, computeShaderExecutorObject);

            TerrainDetailGeneratorConfiguration generatorConfiguration = new TerrainDetailGeneratorConfiguration()
            {
                TerrainDetailImageSideDisjointResolution = 240
            };
            TextureWithCoords fullFundationData = new TextureWithCoords(new TextureWithSize()
            {
                Texture = mainTexture,
                Size = new IntVector2(mainTexture.width, mainTexture.height)
            }, new MyRectangle(0, 0, 3601 * 24, 3601 * 24));

            TerrainDetailGenerator generator =
                new TerrainDetailGenerator(generatorConfiguration, utTextureRendererProxy, fullFundationData,
                    featureAppliers, commonExecutorUtProxy);
            return generator;
        }

        public static TerrainDetailProvider CreateTerrainDetailProvider(
            TerrainDetailGenerator generator, CommonExecutorUTProxy commonExecutorUtProxy, bool useTextureSavingToDisk = false, 
            string terrainDetailFilePath = "C\\unityCache\\", TerrainDetailCornerMerger cornerMerger = null, bool useTextureLoadingFromDisk = false)
        {
            var terrainDetailProviderConfiguration = new TerrainDetailProviderConfiguration()
            {
                UseTextureSavingToDisk = useTextureSavingToDisk,
                UseTextureLoadingFromDisk = useTextureLoadingFromDisk,
                MergeTerrainDetail = cornerMerger != null
            };
            var terrainDetailFileManager = new TerrainDetailFileManager(terrainDetailFilePath, commonExecutorUtProxy);

            var provider =
                new TerrainDetailProvider(terrainDetailProviderConfiguration, terrainDetailFileManager, generator, cornerMerger, new TerrainDetailAlignmentCalculator(240));
            return provider;
        }
    }
}