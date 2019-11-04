using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.Roads.Pathfinding.Fitting;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using NetTopologySuite.Geometries;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class MainStainTerrainProviderDebugObject : MonoBehaviour
    {
        public GameObject RenderTextureGameObject;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            GeoCoordsToUnityTranslator translator = GeoCoordsToUnityTranslator.DefaultTranslator;
            var geoPos1 = new GeoCoordinate(49.600f, 19.543f);
            var lifePos1 = translator.TranslateToUnity(geoPos1);
            var geoPos2 = new GeoCoordinate(49.610f, 19.552f);
            var lifePos2 = translator.TranslateToUnity(geoPos2);
            MyRectangle territoryUsedToCreateStainTexture =
                new MyRectangle(lifePos1.x, lifePos1.y, lifePos2.x - lifePos1.x, lifePos2.y - lifePos1.y);

            //var width = (48609 - 48157);
            //var height = 52243 - 51832;
            //territoryUsedToCreateStainTexture = new UnityCoordsPositions2D(
            //        48157,   51832, width, height
            //    );

            StainTerrainResourceCreatorUTProxy stainTerrainResourceCreator =
                new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator());

            RoadToBiomesConventer roadToBiomesConventer =
                new RoadToBiomesConventer(new RoadDatabaseProxy(new RoadDatabase(@"C:\inz\wrt\")),
                    new RoadToBiomesConventerConfiguration()
                    {
                        PathBiomePriority = 9999
                    }
                );

            HabitatToStainBiomeConventer habitatToBiomesConventer = new HabitatToStainBiomeConventer(
                new HabitatMapDbProxy(new HabitatMapDb(new HabitatMapDb.HabitatMapDbInitializationInfo()
                {
                    RootSerializationPath = @"C:\inz\habitating2\"
                })), HabitatToStainBiomeDebugObject.CreateDebugConversionConfiguration());

            var terrainProvider = new StainTerrainProvider(
                stainTerrainResourceCreator,
                roadToBiomesConventer,
                habitatToBiomesConventer,
                new StainTerrainProviderConfiguration()
                {
                    BiomesContainerConfiguration = new BiomesContainerConfiguration()
                    {
                        HighQualityQueryDistance = 99999.99f,
                        DefaultType = BiomeType.Grass
                    },
                    StainTerrainCoords = territoryUsedToCreateStainTexture,
                    RoadDisplayingAreaNormalizedCoords = new MyRectangle(0.25f, 0.25f, 0.5f, 0.5f),
                    StainTerrainArrayFromBiomesGeneratorConfiguration =
                        new StainTerrainArrayFromBiomesGeneratorConfiguration()
                        {
                            TexturesSideLength = 32,
                            PriorityWeightFactor = 5
                        },
                    BiomeGenerationTemplates = BiomeGenerationDebugObject.GenerateDebugBiomeInstanceDetailTemplates()
                });

            var generator = terrainProvider.ProvideGeneratorAsync().Result;
            var generatedResource = generator.GenerateTerrainTextureDataAsync().Result;

            var fileManager = new StainTerrainResourceFileManager(@"C:\inz\ring1\", new CommonExecutorUTProxy());
            fileManager.SaveResources(generatedResource);

            generatedResource = fileManager.LoadResources().Result;

            var newMaterial = new Material(Shader.Find("Custom/TerrainTextureTest2"));
            BiomeGenerationDebugObject.ConfigureMaterial(generatedResource, newMaterial);
            RenderTextureGameObject.GetComponent<MeshRenderer>().material = newMaterial;
        }
    }

    public class StainTerrainProvider
    {
        private StainTerrainResourceCreatorUTProxy _stainTerrainResourceCreator;
        private RoadToBiomesConventer _roadToBiomesConventer;
        private HabitatToStainBiomeConventer _habitatToBiomesConventer;
        private StainTerrainProviderConfiguration _configuration;

        public StainTerrainProvider(
            StainTerrainResourceCreatorUTProxy stainTerrainResourceCreator,
            RoadToBiomesConventer roadToBiomesConventer,
            HabitatToStainBiomeConventer habitatToBiomesConventer,
            StainTerrainProviderConfiguration configuration)
        {
            _stainTerrainResourceCreator = stainTerrainResourceCreator;
            _roadToBiomesConventer = roadToBiomesConventer;
            _habitatToBiomesConventer = habitatToBiomesConventer;
            _configuration = configuration;
        }

        public async Task<ComputationStainTerrainResourceGenerator> ProvideGeneratorAsync()
        {
            BiomeInstancesContainer container =
                new BiomeInstancesContainer(_configuration.BiomesContainerConfiguration);
            var generationArea = _configuration.StainTerrainCoords;

            (await _roadToBiomesConventer.GenerateBiomesAsync(
                RectangleUtils.CalculateSubPosition(generationArea,
                    _configuration.RoadDisplayingAreaNormalizedCoords))).ForEach(c => container.AddBiome(c));

            (await _habitatToBiomesConventer.FromHabitatGenerateBiomes(generationArea))
                .ForEach(c => container.AddBiome(c));

            var stainTerrainArrayGenerator = new StainTerrainArrayFromBiomesGenerator(
                container,
                new BiomeInstanceDetailGenerator(_configuration.BiomeGenerationTemplates),
                new StainSpaceToUnitySpaceTranslator(_configuration.StainTerrainCoords),
                _configuration.StainTerrainArrayFromBiomesGeneratorConfiguration
            );

            var resourceGenerator = new ComputationStainTerrainResourceGenerator(
                new StainTerrainResourceComposer(_stainTerrainResourceCreator),
                new StainTerrainArrayMelder(),
                stainTerrainArrayGenerator);

            return resourceGenerator;
        }
    }

    public class StainTerrainProviderConfiguration
    {
        public MyRectangle StainTerrainCoords;

        public BiomesContainerConfiguration BiomesContainerConfiguration = new BiomesContainerConfiguration()
        {
            Center = new Vector2(0.5f, 0.5f),
            DefaultType = BiomeType.Forest,
            HighQualityQueryDistance = 999999.1f
        };

        public Dictionary<BiomeType, BiomeInstanceDetailTemplate> BiomeGenerationTemplates;
        public StainTerrainArrayFromBiomesGeneratorConfiguration StainTerrainArrayFromBiomesGeneratorConfiguration;
        public MyRectangle RoadDisplayingAreaNormalizedCoords;
    }

    public class RoadToBiomesConventer
    {
        private RoadDatabaseProxy _roadDatabase;
        private RoadToBiomesConventerConfiguration _configuration;
        private uint _lastBiomeInstanceId = 1500;
        private LineStringToBoxesPathConventer _lineStringToBoxesPathConventer = new LineStringToBoxesPathConventer();

        public RoadToBiomesConventer(RoadDatabaseProxy roadDatabase, RoadToBiomesConventerConfiguration configuration)
        {
            _roadDatabase = roadDatabase;
            _configuration = configuration;
        }

        public async Task<List<BiomeInstanceInfo>> GenerateBiomesAsync(MyRectangle queryArea)
        {
            var generatedBiomes = new List<BiomeInstanceInfo>();
            var paths = await _roadDatabase.Query(queryArea);

            var simplifier = new PathSimplifier(0.1f);
            foreach (var aPathNodes in paths.SelectMany(c => CutToQueryArea(queryArea, c.PathNodes)).ToList())
            {
                var aPathNodes2 = simplifier.Simplify(aPathNodes);
                var generatedPolygons = _lineStringToBoxesPathConventer.Convert(
                    new LineString(aPathNodes2.Select(c => MyNetTopologySuiteUtils.ToCoordinate(c)).ToArray()),
                    _configuration.PathWidth);

                generatedBiomes.Add(new PathBiomeInstanceInfo(generatedPolygons, _configuration.PathBiomeType,
                    _configuration.PathBiomePriority, new BiomeInstanceId(_lastBiomeInstanceId++)));
            }
            return generatedBiomes;
        }

        private List<List<Vector2>> CutToQueryArea(MyRectangle queryArea, List<Vector2> oldNodesList)
        {
            var outList = new List<List<Vector2>>();
            var currentPath = new List<Vector2>();
            foreach (var node in oldNodesList)
            {
                if (queryArea.Contains(node))
                {
                    currentPath.Add(node);
                }
                else
                {
                    if (currentPath.Count > 1)
                    {
                        outList.Add(currentPath);
                    }
                    currentPath = new List<Vector2>();
                }
            }

            if (currentPath.Count > 1)
            {
                outList.Add(currentPath);
            }
            return outList;
        }
    }

    public class RoadToBiomesConventerConfiguration
    {
        public BiomeType PathBiomeType = BiomeType.Sand;
        public int PathBiomePriority = 9;
        public float PathWidth = 1;
    }
}