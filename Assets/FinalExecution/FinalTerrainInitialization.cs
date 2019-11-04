using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.GRing.DynamicLod;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.MT;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.Heightmaps.Welding;
using Assets.PreComputation;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Ring2;
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
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class FinalTerrainInitialization
    {
        private UltraUpdatableContainer _ultraUpdatableContainer;
        private GameInitializationFields _gameInitializationFields;
        private FEConfiguration _configuration;
        private FeGRingConfiguration _gRingConfiguration;

        public FinalTerrainInitialization(UltraUpdatableContainer ultraUpdatableContainer,
            GameInitializationFields gameInitializationFields, FEConfiguration configuration, FeGRingConfiguration gRingConfiguration)
        {
            _ultraUpdatableContainer = ultraUpdatableContainer;
            _gameInitializationFields = gameInitializationFields;
            _configuration = configuration;
            _gRingConfiguration = gRingConfiguration;
        }

        public void Start()
        {
            InitializeWelding();
            /// /// VISIBILITY TEXTURE
            var visibilityTextureSideLength = 16;
            var visibilityTexture = new Texture2D(visibilityTextureSideLength, visibilityTextureSideLength,
                TextureFormat.RFloat, false);
            visibilityTexture.filterMode = FilterMode.Point;

            var visibilityTextureProcessorProxy =
                new Ring1VisibilityTextureProcessorUTProxy(new Ring1VisibilityTextureProcessor(visibilityTexture));
            _ultraUpdatableContainer.Add(visibilityTextureProcessorProxy);

            var visibilityTextureChangeGrabber = new Ring1VisibilityTextureChangeGrabber();

            var terrainParentGameObject = new GameObject("TerrainParent");
            terrainParentGameObject.transform.localPosition = new Vector3(0, 0, 0);

            var globalSideLength = _gRingConfiguration.Ring1GlobalSideLength;
            var globalSize = new Vector2(globalSideLength, globalSideLength);
            var unityCoordsCalculator = new UnityCoordsCalculator(globalSize);
            var orderGrabber = new Ring1PaintingOrderGrabber();

            var painterProxy = new RingTerrainPainterUTProxy(new RingTerrainPainter(_gRingConfiguration.MakeTerrainVisible));
            _ultraUpdatableContainer.Add(painterProxy);

            painterProxy.Update();

            var mainRespondingProxy = new Ring1NodeEventMainRespondingProxy(new Ring1NodeEventMainResponder());
            _ultraUpdatableContainer.AddOtherThreadProxy(new OtherThreadProxyWithPerPostAction()
            {
                OtherThreadProxy = mainRespondingProxy,
                PerPostAction =
                    () =>
                    {
                        var delta = visibilityTextureChangeGrabber.RetriveVisibilityChanges();

                        if (delta.AnyChange)
                        {
                            var visibilityTextureChagnes =
                                visibilityTextureChangeGrabber.RetriveVisibilityChanges();
                            visibilityTextureProcessorProxy.AddOrder(visibilityTextureChagnes);
                        }

                        if (orderGrabber.IsAnyOrder)
                        {
                            painterProxy.AddOrder(orderGrabber.RetriveOrderAndClear());
                        }
                    }
            });

            var stainTerrainResourceCreatorUtProxy =
                new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator());
            _ultraUpdatableContainer.Add(stainTerrainResourceCreatorUtProxy);

            var stainTerrainServiceProxy = new StainTerrainServiceProxy(
                new StainTerrainService(
                    new FromFileStainTerrainResourceGenerator(_configuration.StainTerrainServicePath, _gameInitializationFields.Retrive<CommonExecutorUTProxy>()),
                    //new ComputationStainTerrainResourceGenerator(
                    //    new StainTerrainResourceComposer(
                    //        _stainTerrainResourceCreatorUtProxy
                    //        ),
                    //    new StainTerrainArrayMelder(),
                    //    new DummyStainTerrainArrayFromBiomesGenerator(
                    //        new DebugBiomeContainerGenerator().GenerateBiomesContainer(new BiomesContainerConfiguration()),
                    //        new StainTerrainArrayFromBiomesGeneratorConfiguration()
                    //    )),
                    _gRingConfiguration.Ring1GenerationArea));

            _ultraUpdatableContainer.AddOtherThreadProxy( stainTerrainServiceProxy);

            var ring1Tree = new Ring1Tree(_gRingConfiguration.Ring1TreeConfiguration);
            var ring1TreeProxy = new Ring1TreeProxy(ring1Tree);
            _gameInitializationFields.Retrive<LateAssignBox<Ring1TreeProxy>>().Set(ring1TreeProxy);
            _ultraUpdatableContainer.AddOtherThreadProxy(ring1TreeProxy);

            var terrainShapeDbInitialization = new FETerrainShapeDbInitialization(_ultraUpdatableContainer,
                _gameInitializationFields, _configuration, new FilePathsConfiguration());
            terrainShapeDbInitialization.Start();


            var gRing0NodeTerrainCreator = new GRing0NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                _gameInitializationFields.Retrive<ITerrainShapeDb>(),
                unityCoordsCalculator,
                _gameInitializationFields.Retrive<GRingSpotUpdater>(),
                _gameInitializationFields.Retrive<HeightArrayWeldingPack>(),
                _gRingConfiguration.GroundShapeProviderConfiguration,
                _gRingConfiguration.TerrainMeshProviderConfiguration);

            var gRing1NodeTerrainCreator = new GRing1NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                _gameInitializationFields.Retrive<ITerrainShapeDb>(),
                stainTerrainServiceProxy,
                unityCoordsCalculator,
                _gameInitializationFields.Retrive<GRingSpotUpdater>(),
                _gameInitializationFields.Retrive<HeightArrayWeldingPack>(),
                _gRingConfiguration.GroundShapeProviderConfiguration,
                _gRingConfiguration.TerrainMeshProviderConfiguration);

            var gRing2PatchesCreator = CreateRing2PatchesCreator2();
            var gRing2PatchesCreatorProxy = new GRing2PatchesCreatorProxy(gRing2PatchesCreator);
            _ultraUpdatableContainer.AddOtherThreadProxy(gRing2PatchesCreatorProxy);


            var gRing2NodeTerrainCreator = new GRing2NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                _gameInitializationFields.Retrive<ITerrainShapeDb>(),
                unityCoordsCalculator,
                gRing2PatchesCreatorProxy,
                _gameInitializationFields.Retrive<GRingSpotUpdater>(),
                _gameInitializationFields.Retrive<HeightArrayWeldingPack>(),
                _gRingConfiguration.GroundShapeProviderConfiguration,
                _gRingConfiguration.TerrainMeshProviderConfiguration);

            var gDebugNodeTerrainCreator = new GDebugLodNodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                unityCoordsCalculator,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>()
            );

            var gDebugTerrainedNodeTerrainCreator = new GDebugTerrainedLodNodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                unityCoordsCalculator,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                _gameInitializationFields.Retrive<ITerrainShapeDb>(),
                _gRingConfiguration.GroundShapeProviderConfiguration,
                _gameInitializationFields.Retrive<GRingSpotUpdater>()
            );

            UTRing2PlateStamperProxy stamperProxy = new UTRing2PlateStamperProxy(
                new Ring2PlateStamper(_configuration.Ring2PlateStamperConfiguration,
                    _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>()));
            _ultraUpdatableContainer.Add(stamperProxy);

            Ring2PatchStamplingOverseerFinalizer patchStamper = new Ring2PatchStamplingOverseerFinalizer(
                stamperProxy,
                _gameInitializationFields.Retrive<UTTextureRendererProxy>());


            var gStampedRing2NodeTerrainCreator = new GStampedRing2NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                _gameInitializationFields.Retrive<ITerrainShapeDb>(),
                unityCoordsCalculator,
                gRing2PatchesCreatorProxy,
                patchStamper,
                _gameInitializationFields.Retrive<GRingSpotUpdater>(),
                _gameInitializationFields.Retrive<HeightArrayWeldingPack>(),
                _gRingConfiguration.GroundShapeProviderConfiguration,
                _gRingConfiguration.TerrainMeshProviderConfiguration);

            var gCompositeRing2NodeTerrainCreator = new GCompositeRing2NodeTerrainCreator(
                orderGrabber,
                terrainParentGameObject,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                _gameInitializationFields.Retrive<ITerrainShapeDb>(),
                unityCoordsCalculator,
                gRing2PatchesCreatorProxy,
                _gameInitializationFields.Retrive<GRingSpotUpdater>(),
                _gameInitializationFields.Retrive<HeightArrayWeldingPack>(),
                patchStamper,
                _gRingConfiguration.GroundShapeProviderConfiguration,
                _gRingConfiguration.TerrainMeshProviderConfiguration);

            //var gCompositeRing2NodeTerrainCreator = new GCompositeRing2NodeTerrainCreator(
            //    new List<INewGRingListenersCreator>()
            //    {
            //        gRing2NodeTerrainCreator,
            //        gStampedRing2NodeTerrainCreator,
            //    });

            var subCreator = new SupremeGRingNodeTerrainCreator(new List<NewListenersCreatorWithLimitation>()
            {
                //new NewListenersCreatorWithLimitation()
                //{
                //    Creator = gDebugTerrainedNodeTerrainCreator,
                //    MaximumLod = new FlatLod(14),
                //    //PositionLimiter = new NewListenersCreatorPositionLimiter(new Vector2(0.5f, 0.6f), 0.025f)
                //},

                //new NewListenersCreatorWithLimitation()
                //{
                //    Creator = gRing0NodeTerrainCreator,
                //    MaximumLod = new FlatLod(5),
                //},

                new NewListenersCreatorWithLimitation()
                {
                    Creator = new GVoidNodeTerrainCreator(),
                    MaximumLod = new FlatLod(8),
                    //IsFallthroughCreator = true
                },

                new NewListenersCreatorWithLimitation()
                {
                    Creator = gRing1NodeTerrainCreator,
                    MaximumLod = new FlatLod(10)
                },

                new NewListenersCreatorWithLimitation()
                {
                    Creator = gRing2NodeTerrainCreator,
                    MaximumLod = new FlatLod(14)
                },

                new NewListenersCreatorWithLimitation()
                {
                    Creator = gStampedRing2NodeTerrainCreator,
                    MaximumLod = new FlatLod(13)
                },

                new NewListenersCreatorWithLimitation()
                {
                    Creator = gCompositeRing2NodeTerrainCreator,
                    MaximumLod = new FlatLod(14)
                } 

                //new NewListenersCreatorWithLimitation()
                //{
                //    Creator = gDebugNodeTerrainCreator,
                //    MaximumLod = new FlatLod(11)
                //},
            });

            var eventCollector = new Ring1NodeEventCollector(
                new DynamicFlatLodGRingNodeTerrainCreator(subCreator,
                    new FlatLodCalculator(unityCoordsCalculator, _gRingConfiguration.FlatLodConfiguration)));

            _ultraUpdatableContainer.AddOtherThreadProxy( 
                new OtherThreadProxyWithPerPostAction()
                    {
                        OtherThreadProxy = ring1TreeProxy,
                        PerPostAction = () =>
                        {
                            if (eventCollector.Any)
                            {
                                mainRespondingProxy.AddOrder(eventCollector.RetriveOrderAndClear());
                            }
                        }
                    }
                );
            var repositioner = _gameInitializationFields.Retrive<Repositioner>();
            _ultraUpdatableContainer.AddUpdatableElement(new FieldBasedUltraUpdatable()
            {
                UpdateCameraField = (camera) =>
                {
                    if (_configuration.UpdateRingTree)
                    {
                        ring1TreeProxy.UpdateCamera(
                            FovData.FromCamera(camera, repositioner));
                    }
                }
            });

            _ultraUpdatableContainer.AddUpdatableElement(new FieldBasedUltraUpdatable()
            {
                StartCameraField = (camera) =>
                {
                    ring1TreeProxy.CreateHeightmap(
                        new Ring1Tree.RootNodeCreationParameters()
                        {
                            InitialCameraPosition = repositioner.InvMove(camera.transform.localPosition),
                            NodeListener = eventCollector,
                            PrecisionDistances = _gRingConfiguration.QuadLodPrecisionDistances,
                            UnityCoordsCalculator = unityCoordsCalculator
                        });
                }
            });
        }

        private GRing2PatchesCreator CreateRing2PatchesCreator2()
        {
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            var conciever = _gameInitializationFields.Retrive<TextureConcieverUTProxy>();
            var detailEnhancer =
                new Ring2IntensityPatternEnhancer(_gameInitializationFields.Retrive<UTTextureRendererProxy>(),
                    _gRingConfiguration.Ring2IntensityPatternEnhancingSizeMultiplier);

            var ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(new Ring2PatchesPainter(
                new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));
            _ultraUpdatableContainer.Add(ring2PatchesPainterUtProxy);

            return new GRing2PatchesCreator(
                _gameInitializationFields.Retrive<Ring2RegionsDatabase>(),
                new GRing2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever, detailEnhancer),
                new GRing2Deviser(),
                _configuration.Ring2PatchesOverseerConfiguration
            );
        }

        private void InitializeWelding()
        {
            var weldingTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RFloat);
            weldingTexture.enableRandomWrite = true;
            weldingTexture.filterMode = FilterMode.Point;
            weldingTexture.wrapMode = TextureWrapMode.Clamp;
            weldingTexture.Create();

            HeightArrayWelderProxy welderProxy = new HeightArrayWelderProxy(
                new HeightArrayWeldingDispatcher(
                    new WeldMapLevel1Manager(
                        new WeldMapLevel2Manager(
                            new WeldingExecutor(
                                _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>(),
                                _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(),
                                weldingTexture),
                            new WeldMapLevel2ManagerConfiguration()
                            {
                                WeldTextureSize = new IntVector2(1024, 1024),
                                ColumnsCount = 1024,
                                StripLength = 240,
                                StripsPerOneColumnCount = 2, //todo
                                StripStride = 256
                            }))));

            _ultraUpdatableContainer.AddOtherThreadProxy(welderProxy);

            var terrainThreadAssignBox = new LateAssignBox<Ring1TreeProxy>();
            _gameInitializationFields.SetField(terrainThreadAssignBox);

            var weldingPack = new HeightArrayWeldingPack(new TextureWithSize()
            {
                Texture = weldingTexture,
                Size = new IntVector2(1024, 1024)
            }, welderProxy, _gameInitializationFields.Retrive<CommonExecutorUTProxy>(), _gRingConfiguration.WeldingEnabled);
            _gameInitializationFields.SetField(weldingPack);
        }

    }
}