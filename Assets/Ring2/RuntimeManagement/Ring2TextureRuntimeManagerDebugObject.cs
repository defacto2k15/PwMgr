using System.Collections.Generic;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Ring2.RegionSpace;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Ring2.Stamping;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Ring2.RuntimeManagement
{
    public class Ring2TextureRuntimeManagerDebugObject : MonoBehaviour
    {
        private Ring2PatchesPainterUTProxy _ring2PatchesPainterUtProxy;
        private bool _noMultithreading = true;
        private RandomFieldFigureGeneratorUTProxy _randomFieldFigureGeneratorUtProxy;
        private UTRing2PlateStamperProxy _utRing2PlateStamperProxy;
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Update()
        {
            _ring2PatchesPainterUtProxy.Update();
            _randomFieldFigureGeneratorUtProxy.Update();
            _utRing2PlateStamperProxy.Update();
            if (Input.GetKey(KeyCode.A))
            {
                UnityEngine.Debug.Log("T98 " + DebugMTIncrement.GenerateResults());
            }
        }

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(!_noMultithreading);

            var ring2MeshRepository = Ring2PlateMeshRepository.Create();
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            TextureConcieverUTProxy conciever =
                (new GameObject("TextureConciever", typeof(TextureConcieverUTProxy)))
                .GetComponent<TextureConcieverUTProxy>();

            _ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(
                new Ring2PatchesPainter(
                    new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames))
            );

            VisibleRing2PatchesContainer visiblePatchesContainer = new VisibleRing2PatchesContainer();

            var pixelsPerUnit = 8;
            _randomFieldFigureGeneratorUtProxy = new RandomFieldFigureGeneratorUTProxy(
                new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                    new Ring2RandomFieldFigureGeneratorConfiguration()
                    {
                        PixelsPerUnit = new Vector2(pixelsPerUnit, pixelsPerUnit)
                    }));
            _randomFieldFigureGeneratorUtProxy.Start();

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(_randomFieldFigureGeneratorUtProxy,
                new Ring2RandomFieldFigureRepositoryConfiguration(pixelsPerUnit, new Vector2(10, 10)));

            _utRing2PlateStamperProxy = new UTRing2PlateStamperProxy(new Ring2PlateStamper(
                new Ring2PlateStamperConfiguration()
                {
                    PlateStampPixelsPerUnit = new Dictionary<int, float>()
                    {
                    }
                }, ComputeShaderContainer));

            Quadtree<Ring2Region> regionsTree = Ring2TestUtils.CreateRegionsTreeWithPath(randomFieldFigureRepository);
            Ring2PatchesOverseer patchesOverseer = new Ring2PatchesOverseer(
                new MonoliticRing2RegionsDatabase(regionsTree),
                new Ring2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever),
                new Ring2Deviser(ring2MeshRepository, Repositioner.Default),
                _ring2PatchesPainterUtProxy,
                new Ring2PatchesOverseerConfiguration()
                {
                    IntensityPatternPixelsPerUnit = new Dictionary<int, float>()
                    {
                        {1, 8f}
                    },
                    PatchSize = new Vector2(5, 5)
                },
                new Ring2PatchStamplingOverseerFinalizer(
                    _utRing2PlateStamperProxy,
                    new UTTextureRendererProxy(new TextureRendererService(
                        new MultistepTextureRenderer(ComputeShaderContainer), new TextureRendererServiceConfiguration()
                        {
                            StepSize = new Vector2(500, 500)
                        })), new CommonExecutorUTProxy())
            );

            var overseerProxy = new Ring2PatchesOverseerProxy(patchesOverseer);
            overseerProxy.StartThreading();

            Ring2TextureRuntimeManagerConfiguration managerConfiguration = new Ring2TextureRuntimeManagerConfiguration()
            {
                CreationRectangleSize = new Vector2(10, 10),
                PatchSize = new Vector2(10, 10),
                RemovalRectangleSize = new Vector2(40, 40)
            };

            Ring2TextureRuntimeManager manager = new Ring2TextureRuntimeManager(visiblePatchesContainer,
                managerConfiguration, overseerProxy);

            manager.Start(new Vector2(0, 0));
        }


        public static Quadtree<Ring2Region> CreateRegionsTreeA()
        {
            Ring2AreaDistanceDatabase distanceDatabase = new Ring2AreaDistanceDatabase();
            var groundFabric = new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(0.2f, 0, 0),
                new Color(0.5f, 0, 0),
                new Color(0.7f, 0, 0),
                new Color(1, 0, 0),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 1,1);

            var dotFabric = new Ring2Fabric(Ring2Fiber.DottedTerrainFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(0, 0.2f, 0),
                new Color(0, 0.5f, 0),
                new Color(0, 0.7f, 0),
                new Color(0, 1, 0),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(2.03f, distanceDatabase), 1,1);

            var grassFabric = new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(0, 0, 0.2f),
                new Color(0, 0, 0.5f),
                new Color(0, 0, 0.7f),
                new Color(0, 0, 1),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 1,1);

            var drySandFabric = new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(1, 0, 0.2f),
                new Color(1, 0, 0.5f),
                new Color(1, 0, 0.7f),
                new Color(1, 0, 1),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 1,1);


            var region1Area = RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new[]
            {
                new Vector2(-50, -50),
                new Vector2(50, -50),
                new Vector2(50, 50),
                new Vector2(-50, 5),
            }));

            var region1Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                groundFabric,
                dotFabric
            });

            var region1 = new Ring2Region(region1Area, region1Substance, 2);

            var region2Area = RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new Vector2[]
            {
                new Vector2(5, 5),
                new Vector2(20, 5),
                new Vector2(20, 8),
                new Vector2(5, 8),
            }));

            var region2Substance = new Ring2Substance(new List<Ring2Fabric>
            {
                grassFabric,
                drySandFabric
            });

            var region2 = new Ring2Region(region2Area, region2Substance, 0);


            var defaultFabric = new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
                {
                    new Color(1, 0, 1),
                    new Color(1, 0, 1),
                    new Color(1, 0, 1),
                    new Color(1, 0, 1),
                }),
                new ContantRing2IntensityProvider(),
                1,1);

            //////REGION 3
            var region3Area = RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new[]
            {
                new Vector2(-20, 0),
                new Vector2(0, -20),
                new Vector2(10, 0),
                new Vector2(8, 0),
                new Vector2(0, -18),
                new Vector2(-18, 0),
                new Vector2(0, 18),
                new Vector2(0, 20),
            }));


            var grassFabric2 = new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
                {
                    new Color(1, 1, 0.2f),
                    new Color(1, 1, 0.5f),
                    new Color(1, 1, 0.7f),
                    new Color(1, 0.8f, 0.8f),
                }), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase),
                1,1);

            var region3Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                drySandFabric,
                grassFabric2
            });

            var region3 = new Ring2Region(region3Area, region3Substance, 10);


            var defaultSubstance = new Ring2Substance(new List<Ring2Fabric>()
            {
                defaultFabric
            });

            Quadtree<Ring2Region> regionsTree = new Quadtree<Ring2Region>();
            regionsTree.Insert(region1.RegionEnvelope, region1);
            regionsTree.Insert(region2.RegionEnvelope, region2);
            regionsTree.Insert(region3.RegionEnvelope, region3);
            return regionsTree;
        }

        public static Quadtree<Ring2Region> CreateRegionsTree()
        {
            Ring2AreaDistanceDatabase distanceDatabase = new Ring2AreaDistanceDatabase();
            var groundFabric = new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(0.2f, 0, 0),
                new Color(0.5f, 0, 0),
                new Color(0.7f, 0, 0),
                new Color(1, 0, 0),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 1,1);


            var region1Area = RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new[]
            {
                new Vector2(-100, -10),
                new Vector2(100, -10),
                new Vector2(100, 100),
                new Vector2(-100, 100),
            }));

            var region1Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                groundFabric,
            });

            var region1 = new Ring2Region(region1Area, region1Substance, 2);


            var drySandFabric = new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(0, 0, 0.2f),
                new Color(0, 0, 0.5f),
                new Color(0, 0, 0.7f),
                new Color(0, 0, 1),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 1,1);

            var region2Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                drySandFabric
            });

            var region2 = new Ring2Region(RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new Vector2[]
            {
                new Vector2(-15, 0),
                new Vector2(0, 15),
                new Vector2(15, 0),
                new Vector2(0, -15),
            })), region2Substance, 3);


            Quadtree<Ring2Region> regionsTree = new Quadtree<Ring2Region>();
            regionsTree.Insert(region1.RegionEnvelope, region1);
            regionsTree.Insert(region2.RegionEnvelope, region2);
            return regionsTree;
        }
    }
}