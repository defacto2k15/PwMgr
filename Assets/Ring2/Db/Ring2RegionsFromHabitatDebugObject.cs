using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Devising;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RegionsToPatchTemplate;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Ring2.Db
{
    public class Ring2RegionsFromHabitatDebugObject : MonoBehaviour
    {
        private Ring2PatchesOverseer _ring2PatchesOverseer;
        private MyRectangle _generationArea;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var msw = new MyStopWatch();
            msw.StartSegment("Precomputing!");

            var ring2MeshRepository = Ring2PlateMeshRepository.Create();
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            TextureConcieverUTProxy conciever =
                (new GameObject("TextureConciever", typeof(TextureConcieverUTProxy)))
                .GetComponent<TextureConcieverUTProxy>();

            var ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(
                new Ring2PatchesPainter(
                    new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));

            Ring2RandomFieldFigureGenerator figureGenerator = new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(4, 4)
                });
            var utFigureGenerator = new RandomFieldFigureGeneratorUTProxy(figureGenerator);

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(utFigureGenerator,
                new Ring2RandomFieldFigureRepositoryConfiguration(2, new Vector2(20, 20)));


            GeoCoordsToUnityTranslator translator = GeoCoordsToUnityTranslator.DefaultTranslator;
            //var geoPos1 = new GeoCoordinate(49.6108832, 19.5435037);
            //var lifePos1 = translator.TranslateToUnity(geoPos1);
            //var geoPos2 = new GeoCoordinate(49.612561, 19.546638);
            //var lifePos2 = translator.TranslateToUnity(geoPos2);

            var lifePos1 = new Vector2(90 * 520, 90 * 585);
            var lifePos2 = lifePos1 + new Vector2(90 * 4, 90 * 4);
            _generationArea = new MyRectangle(lifePos1.x, lifePos1.y, lifePos2.x - lifePos1.x,
                lifePos2.y - lifePos1.y);
            _generationArea = RectangleUtils.CalculateSubPosition(_generationArea,
                new MyRectangle(0.0f, 0.0f, 1, 1));

            var patchSize = new Vector2(90, 90);
            _generationArea = AlignGenerationAreaToPatch(_generationArea, patchSize);

            Debug.Log("Generation area results: " + _generationArea);


            var habitatMap = new HabitatMapDbProxy(new HabitatMapDb(new HabitatMapDb.HabitatMapDbInitializationInfo()
            {
                RootSerializationPath = @"C:\inz\habitating2\"
            }));

            var configuration = new Ring2RegionsDbGeneratorConfiguration()
            {
                FromHabitatTemplates = CreateFromHabitatTemplates(new Ring2AreaDistanceDatabase())
            };

            var regionsDbGenerator =
                new Ring2RegionsDbGenerator(habitatMap, configuration);
            var regionsDatabase = regionsDbGenerator.GenerateDatabaseAsync(_generationArea).Result;

            _ring2PatchesOverseer = new Ring2PatchesOverseer(
                regionsDatabase,
                new Ring2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever),
                new Ring2Deviser(ring2MeshRepository, Repositioner.Default),
                ring2PatchesPainterUtProxy,
                new Ring2PatchesOverseerConfiguration()
                {
                    IntensityPatternPixelsPerUnit = new Dictionary<int, float>()
                    {
                        {1, 1 / 9f}
                    },
                    PatchSize = patchSize
                }
            );
            msw.StartSegment("Main");
            Debug.Log($"Size: {_generationArea.Width} ,h:{_generationArea.Height}");
            GeneratePatches();

            UnityEngine.Debug.Log(msw.CollectResults());
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                GeneratePatches();
            }
        }

        private void GeneratePatches()
        {
            _ring2PatchesOverseer.ProcessOrderAsync(new Ring2PatchesOverseerOrder()
            {
                CreationOrder = new List<Ring2PatchesCreationOrderElement>()
                {
                    new Ring2PatchesCreationOrderElement()
                    {
                        Rectangle = _generationArea.ToRectangle(),
                        OutPatchId = new OverseedPatchId()
                    }
                }
            }).Wait();
        }

        public static MyRectangle AlignGenerationAreaToPatch(MyRectangle oldArea,
            Vector2 patchSize)
        {
            var size = new Vector2(
                Mathf.CeilToInt(oldArea.Width / patchSize.x) * patchSize.x,
                Mathf.CeilToInt(oldArea.Height / patchSize.y) * patchSize.y);
            return new MyRectangle(oldArea.X, oldArea.Y, size.x, size.y);
        }

        public static Dictionary<HabitatType, Ring2RegionFromHabitatTemplate> CreateFromHabitatTemplates(
            Ring2AreaDistanceDatabase distanceDatabase)
        {
            return new Dictionary<HabitatType, Ring2RegionFromHabitatTemplate>()
            {
                {
                    HabitatType.Forest, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(new List<Color>()
                            {
                                new Color(0.2f, 0, 0),
                                new Color(0.5f, 0, 0),
                                new Color(0.7f, 0, 0),
                                new Color(1, 0, 0),
                            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 1,1)
                        },
                        Magnitude = 1,
                        BufferLength = 3
                    }
                },
                {
                    HabitatType.Fell, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(new List<Color>()
                            {
                                new Color(0, 0.2f, 0),
                                new Color(0, 0.5f, 0),
                                new Color(0, 0.7f, 0),
                                new Color(0, 1, 0),
                            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 1,1)
                        },
                        Magnitude = 2,
                        BufferLength = 3
                    }
                },
                {
                    HabitatType.NotSpecified, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(new List<Color>()
                            {
                                new Color(0, 0, 0.2f),
                                new Color(0, 0, 0.5f),
                                new Color(0, 0, 0.7f),
                                new Color(0, 0, 1),
                            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 1,1)
                        },
                        Magnitude = 3,
                        BufferLength = 3
                    }
                },
                {
                    HabitatType.Grassland, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(new List<Color>()
                            {
                                new Color(0.2f, 0, 0.2f),
                                new Color(0.5f, 0, 0.5f),
                                new Color(0.7f, 0, 0.7f),
                                new Color(1, 0, 1),
                            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 1,1)
                        },
                        Magnitude = 3,
                        BufferLength = 3
                    }
                },
                {
                    HabitatType.Meadow, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(new List<Color>()
                            {
                                new Color(0.2f, 0.2f, 0.2f),
                                new Color(0.5f, 0.5f, 0.5f),
                                new Color(0.7f, 0.7f, 0.7f),
                                new Color(1, 1, 1),
                            }), new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 1,1)
                        },
                        Magnitude = 3,
                        BufferLength = 3
                    }
                },
            };
        }
    }
}