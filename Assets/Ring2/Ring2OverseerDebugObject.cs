using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.valTypes;
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
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Ring2
{
    public class Ring2OverseerDebugObject : MonoBehaviour
    {
        private Ring2PatchesPainterUTProxy _ring2PatchesPainterUtProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var ring2MeshRepository = Ring2PlateMeshRepository.Create();
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            TextureConcieverUTProxy conciever =
                (new GameObject("TextureConciever", typeof(TextureConcieverUTProxy)))
                .GetComponent<TextureConcieverUTProxy>();

            _ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(
                new Ring2PatchesPainter(
                    new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));

            Ring2RandomFieldFigureGenerator figureGenerator = new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(2, 2)
                });
            var utFigureGenerator = new RandomFieldFigureGeneratorUTProxy(figureGenerator);

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(utFigureGenerator,
                new Ring2RandomFieldFigureRepositoryConfiguration(2, new Vector2(20, 20)));

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
                }
            );
            var msw = new MyStopWatch();
            msw.StartSegment("Main");
            patchesOverseer.ProcessOrderAsync(
                Ring2TestUtils.CreateCreationOrderOn(new MyRectangle(-25, -25, 50, 50), new Vector2(20, 20))).Wait();
            UnityEngine.Debug.Log(msw.CollectResults());
        }
    }

    public static class Ring2TestUtils
    {
        public static Ring2PatchesOverseerOrder CreateCreationOrderOn(MyRectangle creationArea, Vector2 patchSize)
        {
            List<Ring2PatchesCreationOrderElement> creationOrder = new List<Ring2PatchesCreationOrderElement>();
            for (float x = creationArea.X; x < creationArea.MaxX; x += patchSize.x)
            {
                for (float y = creationArea.Y; y < creationArea.MaxY; y += patchSize.y)
                {
                    creationOrder.Add(new Ring2PatchesCreationOrderElement()
                    {
                        OutPatchId = new OverseedPatchId(),
                        Rectangle = new MyRectangle(x, y, patchSize.x, patchSize.y)
                    });
                }
            }
            return new Ring2PatchesOverseerOrder()
            {
                CreationOrder = creationOrder
            };
        }

        public static Quadtree<Ring2Region> CreateRegionsTreeWithPath(
            Ring2RandomFieldFigureRepository randomFieldFigureRepository)
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
                }),
                //new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 1); 
                new IntensityJittererFromRandomField(
                    new ValuesFromRandomFieldProvider(RandomFieldNature.FractalSimpleValueNoise3, 0,
                        randomFieldFigureRepository),
                    new Vector2(0.2f, 1), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase)),
                1,1);

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

            /// //// REGION 3
            var region3Area = RegionSpaceUtils.ToFatLineString(0.5f, new[]
            {
                new Vector2(-18, -18),
                new Vector2(-8, -18),
                new Vector2(-8, -4),
                new Vector2(4, 4),
                new Vector2(-18, 8),
            });

            var grassyFieldFabric = new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
                {
                    new Color(0, 0.2f, 0),
                    new Color(0, 0.5f, 0),
                    new Color(0, 0.7f, 0),
                    new Color(0, 1, 0),
                }),
                //new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 2); 
                new IntensityJittererFromRandomField(
                    new ValuesFromRandomFieldProvider(RandomFieldNature.FractalSimpleValueNoise3, 0,
                        randomFieldFigureRepository),
                    new Vector2(0.1f, 1.2f), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase)),
                1,1);

            var region3Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                grassyFieldFabric
            });

            var region3 = new Ring2Region(region3Area, region3Substance, 30);


            Quadtree<Ring2Region> regionsTree = new Quadtree<Ring2Region>();
            regionsTree.Insert(region1.RegionEnvelope, region1);
            regionsTree.Insert(region2.RegionEnvelope, region2);
            regionsTree.Insert(region3.RegionEnvelope, region3);
            return regionsTree;
        }


        public static Quadtree<Ring2Region> CreateRegionsTreeWithLinePath(
            Ring2RandomFieldFigureRepository randomFieldFigureRepository,
            IEnumerable<Vector2> pathNodes)
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
                }),
                //new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 1); 
                new IntensityJittererFromRandomField(
                    new ValuesFromRandomFieldProvider(RandomFieldNature.FractalSimpleValueNoise3, 0,
                        randomFieldFigureRepository),
                    new Vector2(0.2f, 1), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase)),
                1,1);

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

            /// //// REGION 3
            var region3Area = RegionSpaceUtils.ToFatLineString(1.5f, pathNodes);

            var grassyFieldFabric = new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
                {
                    new Color(0, 0.2f, 0),
                    new Color(0, 0.5f, 0),
                    new Color(0, 0.7f, 0),
                    new Color(0, 1, 0),
                }),
                //new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 2); 
                new IntensityJittererFromRandomField(
                    new ValuesFromRandomFieldProvider(RandomFieldNature.FractalSimpleValueNoise3, 0,
                        randomFieldFigureRepository),
                    new Vector2(0.8f, 1.2f), new FromAreaEdgeDistanceRing2IntensityProvider(0.7f, distanceDatabase)),
                1,1);

            var region3Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                grassyFieldFabric
            });

            var region3 = new Ring2Region(region3Area, region3Substance, 30);


            Quadtree<Ring2Region> regionsTree = new Quadtree<Ring2Region>();
            regionsTree.Insert(region1.RegionEnvelope, region1);
            regionsTree.Insert(region2.RegionEnvelope, region2);
            regionsTree.Insert(region3.RegionEnvelope, region3);
            return regionsTree;
        }

        public static Quadtree<Ring2Region> CreateRegionsTreeWithPath2(
            Ring2RandomFieldFigureRepository randomFieldFigureRepository)
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
                new Vector2(0, 0),
                new Vector2(140, 0),
                new Vector2(200, 140),
                new Vector2(0, 200),
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
                }),
                //new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 1); 
                new IntensityJittererFromRandomField(
                    new ValuesFromRandomFieldProvider(RandomFieldNature.FractalSimpleValueNoise3, 0,
                        randomFieldFigureRepository),
                    new Vector2(0.2f, 1), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase)),
                1,1);

            var region2Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                drySandFabric
            });

            var region2 = new Ring2Region(RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new Vector2[]
            {
                new Vector2(0, 100),
                new Vector2(100, 200),
                new Vector2(100, 0),
                new Vector2(50, 50),
            })), region2Substance, 3);

            /// //// REGION 3
            var region3Area = RegionSpaceUtils.ToFatLineString(3.5f, new[]
            {
                new Vector2(0, 0),
                new Vector2(50, 50),
                new Vector2(20, 150),
                new Vector2(150, 140),
                new Vector2(110, 8),
            });

            var grassyFieldFabric = new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
                {
                    new Color(0, 0.2f, 0),
                    new Color(0, 0.5f, 0),
                    new Color(0, 0.7f, 0),
                    new Color(0, 1, 0),
                }),
                //new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 2); 
                new IntensityJittererFromRandomField(
                    new ValuesFromRandomFieldProvider(RandomFieldNature.FractalSimpleValueNoise3, 0,
                        randomFieldFigureRepository),
                    new Vector2(0.1f, 1.2f), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase)),
                1,1);

            var region3Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                grassyFieldFabric
            });

            var region3 = new Ring2Region(region3Area, region3Substance, 30);


            Quadtree<Ring2Region> regionsTree = new Quadtree<Ring2Region>();
            regionsTree.Insert(region1.RegionEnvelope, region1);
            regionsTree.Insert(region2.RegionEnvelope, region2);
            regionsTree.Insert(region3.RegionEnvelope, region3);
            return regionsTree;
        }

        public static Quadtree<Ring2Region> CreateRegionsTreeWithPath3(
            Ring2RandomFieldFigureRepository randomFieldFigureRepository)
        {
            Ring2AreaDistanceDatabase distanceDatabase = new Ring2AreaDistanceDatabase();
            var groundFabric = new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(0.2f, 0, 0),
                new Color(0.5f, 0, 0),
                new Color(0.7f, 0, 0),
                new Color(1, 0, 0),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.7f,1);


            var region1Area = RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new[]
            {
                new Vector2(0, 0),
                new Vector2(140, 0),
                new Vector2(200, 140),
                new Vector2(0, 200),
            }));

            var region1Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                groundFabric,
            });

            var region1 = new Ring2Region(region1Area, region1Substance, 1);


            var groundFabric2 = new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
            {
                new Color(0, 0, 0.2f),
                new Color(0, 0, 0.5f),
                new Color(0, 0, 0.7f),
                new Color(0, 0, 1),
            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.35f,1);

            var region2Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                groundFabric2
            });

            var region2 = new Ring2Region(RegionSpaceUtils.Create(MyNetTopologySuiteUtils.ToPolygon(new Vector2[]
            {
                new Vector2(0, 100),
                new Vector2(100, 200),
                new Vector2(100, 0),
                new Vector2(50, 50),
            })), region2Substance, 1);

            /// //// REGION 3
            var region3Area = RegionSpaceUtils.ToFatLineString(3.5f, new[]
            {
                new Vector2(0, 0),
                new Vector2(50, 50),
                new Vector2(20, 150),
                new Vector2(150, 140),
                new Vector2(110, 8),
            });

            var grassyFieldFabric = new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
                {
                    new Color(0, 0.2f, 0),
                    new Color(0, 0.5f, 0),
                    new Color(0, 0.7f, 0),
                    new Color(0, 1, 0),
                }),
                //new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase), 2); 
                new IntensityJittererFromRandomField(
                    new ValuesFromRandomFieldProvider(RandomFieldNature.FractalSimpleValueNoise3, 0,
                        randomFieldFigureRepository),
                    new Vector2(0.6f, 1.2f), new FromAreaEdgeDistanceRing2IntensityProvider(0.3f, distanceDatabase)),
                1,1);

            var region3Substance = new Ring2Substance(new List<Ring2Fabric>()
            {
                grassyFieldFabric
            });

            var region3 = new Ring2Region(region3Area, region3Substance, 30);

            Quadtree<Ring2Region> regionsTree = new Quadtree<Ring2Region>();
            regionsTree.Insert(region1.RegionEnvelope, region1);
            regionsTree.Insert(region2.RegionEnvelope, region2);
            regionsTree.Insert(region3.RegionEnvelope, region3);
            return regionsTree;
        }
    }
}