using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.HeightArrayDbs;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Roads;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.ShaderUtils;
using Assets.TerrainMat;
using Assets.Trees.Db;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement.BiomesMap;
using Assets.Utils;
using Assets.Utils.Quadtree;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Trees.Placement.Habitats
{
    public enum VegetationPlacementPrecision
    {
        High,
        Medium,
        Low
    }

    public class HabitatTreePlacesDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        //todo habitats polygons must have buffering!!
        public void Start()
        {
            var tester = new DebugTerrainTester(ComputeShaderContainer);
            var msw = new MyStopWatch();
            msw.StartSegment("Precomputing");
            tester.Start(false);

            CommonExecutorUTProxy commonExecutor = new CommonExecutorUTProxy();
            HabitatMapDbProxy habitatMapDbProxy = new HabitatMapDbProxy(new HabitatMapDb(
                new HabitatMapDb.HabitatMapDbInitializationInfo()
                {
                    RootSerializationPath = @"C:\inz\habitating2\"
                }));

            var biomeProvidersGenerators = CreateBiomeConfigurationsDict()
                .ToDictionary(c => c.Key, c => new BiomeProvidersFromHabitatGenerator(habitatMapDbProxy, c.Value));

            var genralPlacer = new GeneralMultiDistrictPlacer(new GeneralMultiDistrictPlacerConfiguration()
            {
                GenerationDistrictSize = new Vector2(90, 90),
                PrecisionsPerDistance = new Dictionary<VegetationPlacementPrecision, float>()
                {
                    {VegetationPlacementPrecision.High, 50}, //todo
                    {VegetationPlacementPrecision.Medium, 100},
                    {VegetationPlacementPrecision.Low, 3000}
                },
                PerPrecisionConfigurations = new Dictionary<VegetationPlacementPrecision, PerPrecisionConfiguration>()
                {
                    {
                        VegetationPlacementPrecision.High, new PerPrecisionConfiguration()
                        {
                            BiomeArrayHeightJitterRange = new Vector2(0.9f, 1.1f),
                            BiomeArrayPixelsPerUnit = 0.333f,
                            BiomeArrayRandomPixelsPerUnit = 0.333f,
                            UsedVegetationRanks = VegetationLevelRank.Small.RetriveSameOrBigger(),
                            IntensityPixelsPerUnit = 0.333f,
                            VegetationPlacerConfiguration = new VegetationPlacerConfiguration(),
                            BiomeArrayGenerationTerrainResolution = TerrainCardinalResolution.MIN_RESOLUTION //todo
                        }
                    },
                    {
                        VegetationPlacementPrecision.Medium, new PerPrecisionConfiguration()
                        {
                            BiomeArrayHeightJitterRange = new Vector2(0.9f, 1.1f),
                            BiomeArrayPixelsPerUnit = 0.333f,
                            BiomeArrayRandomPixelsPerUnit = 0.333f,
                            UsedVegetationRanks = VegetationLevelRank.Medium.RetriveSameOrBigger(),
                            IntensityPixelsPerUnit = 0.333f,
                            VegetationPlacerConfiguration = new VegetationPlacerConfiguration(),
                            BiomeArrayGenerationTerrainResolution = TerrainCardinalResolution.MIN_RESOLUTION //todo
                        }
                    },
                    {
                        VegetationPlacementPrecision.Low, new PerPrecisionConfiguration()
                        {
                            BiomeArrayHeightJitterRange = new Vector2(0.9f, 1.1f),
                            BiomeArrayPixelsPerUnit = 0.333f,
                            BiomeArrayRandomPixelsPerUnit = 0.333f,
                            UsedVegetationRanks = VegetationLevelRank.Big.RetriveSameOrBigger(),
                            IntensityPixelsPerUnit = 0.333f,
                            VegetationPlacerConfiguration = new VegetationPlacerConfiguration(),
                            BiomeArrayGenerationTerrainResolution = TerrainCardinalResolution.MIN_RESOLUTION //todo
                        }
                    },
                }
            }, tester.TerrainShapeDbProxy, commonExecutor, biomeProvidersGenerators);

            var vegetationCreationCharacteristicses = CreateRankDependentSpeciesCharacteristics();

            var repositioner = Repositioner.Default;
            var generationCenter = repositioner.InvMove(new Vector2(90 * 4.5f, 90 * 8.5f));
            var generationArea = repositioner.InvMove(new MyRectangle(90 * 8, 90 * 8, 90 * 8, 90 * 8));

            msw.StartSegment("Placing");

            MyStopWatch.AddGlobalStopWatch("BiomesMap");
            var database = genralPlacer.Generate(
                generationArea,
                vegetationCreationCharacteristicses,
                generationCenter);

            msw.StartSegment("RemovingSubjectsOnRoad");
            var vegetationOnRoadRemover = new VegetationOnRoadRemover(
                new RoadDatabaseProxy(new RoadDatabase(@"C:\inz\wrtC\")), new VegetationOnRoadRemoverConfiguration()
                {
                    RemovalCellSize = new Vector2(30, 30),
                    PerDistanceRemovalPropabilities = new Dictionary<VegetationLevelRank, MyRange>()
                    {
                        {VegetationLevelRank.Small, new MyRange(4, 8)},
                        {VegetationLevelRank.Medium, new MyRange(4, 8)},
                        {VegetationLevelRank.Big, new MyRange(4, 8)},
                    }
                });
            var newDb = vegetationOnRoadRemover.RemoveCollidingTrees(database, generationArea);

            Debug.Log(
                $"M54 base databaseCount {database.Subjects.Values.Sum(c => c.QueryAll().Count)}, removedCount: {newDb.Subjects.Values.Sum(c => c.QueryAll().Count)}");

            msw.StartSegment("CreatingDebug");
            CreateDebugObjectsForSubjects(database);

            msw.StartSegment("Writing to file");
            VegetationDatabaseFileUtils.WriteToFile(@"C:\inz\db1.json", newDb);

            Debug.Log("K62: " + msw.CollectResults());
            Debug.Log("L11: " + MyStopWatch.RetriveGlobalStopWatch("BiomesMap").CollectResults());
        }

        public static void CreateDebugObjectsForSubjects(VegetationSubjectsDatabase database)
        {
            foreach (var pair in database.Subjects)
            {
                var rank = pair.Key;
                var rankParent = new GameObject(rank.ToString());
                foreach (var subject in pair.Value.QueryAll())
                {
                    var baseObject = CreateBaseObject(rank);
                    baseObject.GetComponent<Renderer>().material.color = CreateDebugColor(subject);
                    baseObject.transform.position =
                        Repositioner.Default.Move(new Vector3(subject.XzPosition.x, 0, subject.XzPosition.y));
                    baseObject.transform.SetParent(rankParent.transform);

                    var size = subject.CreateCharacteristics.RangedSize;
                    baseObject.transform.localScale = VectorUtils.FillVector3(size * 4);
                }
            }
        }

        private static Color CreateDebugColor(VegetationSubject subject)
        {
            var input = subject.CreateCharacteristics.CurrentVegetationType;
            if (input == VegetationSpeciesEnum.Tree1A)
            {
                return new Color(0, 0.3f, 0);
            }
            if (input == VegetationSpeciesEnum.Tree2A)
            {
                return new Color(0, 0.6f, 0);
            }
            if (input == VegetationSpeciesEnum.Tree3A)
            {
                return new Color(0, 1.0f, 0);
            }

            if (input == VegetationSpeciesEnum.Tree1B)
            {
                return new Color(0.3f, 0, 0);
            }
            if (input == VegetationSpeciesEnum.Tree2B)
            {
                return new Color(0.6f, 0, 0);
            }
            if (input == VegetationSpeciesEnum.Tree3B)
            {
                return new Color(1.0f, 0, 0);
            }

            if (input == VegetationSpeciesEnum.Tree1C)
            {
                return new Color(0, 0, 0.3f);
            }
            if (input == VegetationSpeciesEnum.Tree2C)
            {
                return new Color(0, 0, 0.6f);
            }
            if (input == VegetationSpeciesEnum.Tree3C)
            {
                return new Color(0, 0, 1.0f);
            }
            return new Color(0, 0, 0);
        }

        private static GameObject CreateBaseObject(VegetationLevelRank rank)
        {
            if (rank == VegetationLevelRank.Big)
            {
                return GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }
            else if (rank == VegetationLevelRank.Medium)
            {
                return GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            else
            {
                return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            }
        }


        public static Dictionary<VegetationLevelRank, Dictionary<HabitatType, List<PrioritisedBiomeProvider>>>
            CreateBiomeConfigurationsDict()
        {
            //float perM2 = 60; // to musi! być też per vegetation rank
            //var singleResolutionDict = DebugCreateSingleResolutionBiomesProvider(perM2);

            return new Dictionary<VegetationLevelRank, Dictionary<HabitatType, List<PrioritisedBiomeProvider>>>()
            {
                {VegetationLevelRank.Big, DebugCreateSingleResolutionBiomesProvider(5)},
                {VegetationLevelRank.Medium, DebugCreateSingleResolutionBiomesProvider(15)},
                {VegetationLevelRank.Small, DebugCreateSingleResolutionBiomesProvider(40)},
            };
        }

        private static Dictionary<HabitatType, List<PrioritisedBiomeProvider>>
            DebugCreateSingleResolutionBiomesProvider(float perM2)
        {
            return new Dictionary<HabitatType, List<PrioritisedBiomeProvider>>()
            {
                {
                    HabitatType.Grassland, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            BiomeProvider = new HeightDependentBiomeProvider(
                                new List<VegetationBiomeLevel>()
                                {
                                    new VegetationBiomeLevel(
                                        biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                                        {
                                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1B,
                                                0.7f),
                                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2B,
                                                0.15f),
                                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3B,
                                                0.15f),
                                        },
                                        exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                        maxElementsPerCellPerM2: perM2 / 100f),
                                },
                                new MarginedRange(new Vector2(0.0f, 0.6f), 0.15f)
                            ),
                            Priority = 1
                        }
                    }
                },
                {
                    HabitatType.Forest, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            BiomeProvider =
                                new HeightDependentBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Tree1A, 0.7f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Tree2A, 0.15f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Tree3A, 0.15f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / 100f),
                                    },
                                    new MarginedRange(new Vector2(0.0f, 0.6f), 0.15f)
                                ),
                            Priority = 1
                        }
                    }
                },
                {
                    HabitatType.NotSpecified, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            BiomeProvider =
                                new HeightDependentBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Tree1C, 0.7f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Tree2C, 0.15f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Tree3C, 0.15f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / 100f),
                                    },
                                    new MarginedRange(new Vector2(0.0f, 0.6f), 0.15f)
                                ),
                            Priority = 1
                        }
                    }
                },
            };
        }


        public static Dictionary<VegetationLevelRank, List<VegetationSpeciesCreationCharacteristics>>
            CreateRankDependentSpeciesCharacteristics()
        {
            return new Dictionary<VegetationLevelRank, List<VegetationSpeciesCreationCharacteristics>>()
            {
                {
                    VegetationLevelRank.Big,
                    BiomeMapGenerationTreePlacerDebugObject.CreateSpeciesCreationCharacteristicses(1f)
                },
                {
                    VegetationLevelRank.Medium,
                    BiomeMapGenerationTreePlacerDebugObject.CreateSpeciesCreationCharacteristicses(0.6f)
                },
                {
                    VegetationLevelRank.Small,
                    BiomeMapGenerationTreePlacerDebugObject.CreateSpeciesCreationCharacteristicses(0.3f)
                }
            };
        }
    }


    public class GeneralSingleDistrictTreePlacer
    {
        private VegetationPlacerConfiguration _placerConfiguration;

        private Dictionary<VegetationLevelRank, List<VegetationSpeciesCreationCharacteristics>>
            _creationCharacteristicses;

        public GeneralSingleDistrictTreePlacer(VegetationPlacerConfiguration placerConfiguration,
            Dictionary<VegetationLevelRank, List<VegetationSpeciesCreationCharacteristics>> creationCharacteristicses)
        {
            _placerConfiguration = placerConfiguration;
            _creationCharacteristicses = creationCharacteristicses;
        }

        public VegetationSubjectsDatabase PlaceTrees(
            MyRectangle placingArea,
            List<VegetationLevelRank> ranksToPlace,
            VegetationSpeciesIntensityMap intensityMap,
            Dictionary<VegetationLevelRank, IVegetationBiomesMap> biomesMaps
        )
        {
            VegetationSubjectsDatabase vegetationSubjectsDatabase = new VegetationSubjectsDatabase();

            var placer = new VegetationPlacer(vegetationSubjectsDatabase);

            foreach (var rank in ranksToPlace)
            {
                var creationCharacteristicses = _creationCharacteristicses[rank];
                placer.PlaceVegetation(
                    areaToGenerate: GenerationArea.FromMyRectangle(placingArea),
                    configuration: _placerConfiguration,
                    intensityMap: intensityMap,
                    biomesMap: biomesMaps[rank],
                    creationCharacteristicses: creationCharacteristicses,
                    levelRank: rank,
                    spotInfoProvider: new ConcievingTerrainSpotInfoProvider()
                );
            }

            return vegetationSubjectsDatabase;
        }
    }

    public class GeneralMultiDistrictPlacer
    {
        private GeneralMultiDistrictPlacerConfiguration _configuration;
        private TerrainShapeDbProxy _terrainShapeDbProxy;
        private CommonExecutorUTProxy _commonExecutor;
        private Dictionary<VegetationLevelRank, BiomeProvidersFromHabitatGenerator> _biomeProvidersGenerators;

        public GeneralMultiDistrictPlacer(
            GeneralMultiDistrictPlacerConfiguration configuration,
            TerrainShapeDbProxy terrainShapeDbProxy,
            CommonExecutorUTProxy commonExecutor,
            Dictionary<VegetationLevelRank, BiomeProvidersFromHabitatGenerator> biomeProvidersGeneratorses)
        {
            _configuration = configuration;
            _terrainShapeDbProxy = terrainShapeDbProxy;
            _commonExecutor = commonExecutor;
            _biomeProvidersGenerators = biomeProvidersGeneratorses;
        }

        public VegetationSubjectsDatabase Generate(
            MyRectangle areaToGenerate,
            Dictionary<VegetationLevelRank, List<VegetationSpeciesCreationCharacteristics>>
                vegetationCreationCharacteristicses,
            Vector2 generationCenter
        )
        {
            List<VegetationSubjectsDatabase> generatedDbs = new List<VegetationSubjectsDatabase>();

            var msw = new MyStopWatch();
            var districtSize = _configuration.GenerationDistrictSize;

            int allCellsCount =
                Mathf.CeilToInt((areaToGenerate.Width) / districtSize.x) *
                Mathf.CeilToInt((areaToGenerate.Height) / districtSize.y);
            int cellCount = 0;
            var outLogWriter = new OutLogWriter(@"C:\inz\logs\treeGeneration.txt");
            for (float x = areaToGenerate.X; x < areaToGenerate.MaxX; x += districtSize.x)
            {
                for (float y = areaToGenerate.Y; y < areaToGenerate.MaxY; y += districtSize.y)
                {
                    var districtArea = new MyRectangle(x, y, districtSize.x, districtSize.y);
                    var distanceToGeneratonCenter = Vector2.Distance(districtArea.Center, generationCenter);

                    var precision = PrecisionUtils.RetrivePerDistancePrecision(_configuration.PrecisionsPerDistance,
                        distanceToGeneratonCenter);

                    var perPrecisionConfiguration = _configuration.PerPrecisionConfigurations[precision];

                    var singleCellPlacer = new GeneralSingleDistrictTreePlacer(
                        perPrecisionConfiguration.VegetationPlacerConfiguration,
                        vegetationCreationCharacteristicses);

                    msw.StartSegment("INPlacerPre");

                    var ranksToGenerate = perPrecisionConfiguration.UsedVegetationRanks;

                    var positionRemapper =
                        new PositionRemapper(GenerationArea.FromMyRectangle(districtArea));

                    msw.StartSegment("INPlacerIntensityTex");
                    var intensityTexture = GenerateIntensityTexture(districtArea, perPrecisionConfiguration);
                    var intensityMap = new VegetationSpeciesIntensityMap(intensityTexture, positionRemapper);

                    msw.StartSegment("INPlacerBiomesMap");
                    var biomesMaps = CreateBiomesMaps(ranksToGenerate, districtArea,
                        perPrecisionConfiguration);

                    msw.StartSegment("INPlacerPlacing");
                    var newDistrictDb =
                        singleCellPlacer.PlaceTrees(districtArea, ranksToGenerate, intensityMap, biomesMaps);
                    msw.StartSegment("INPlacerAdding");
                    generatedDbs.Add(newDistrictDb);

                    outLogWriter.Log($"Done {cellCount++}/{allCellsCount}");
                }
            }

            msw.StartSegment("INPlaceFinalAdding");
            var finalDb = new VegetationSubjectsDatabase();
            foreach (var pair in generatedDbs.SelectMany(k => k.Subjects).SelectMany(c => c.Value.QueryAll()
                .Select(k => new
                {
                    rank = c.Key,
                    subjects = k
                })))
            {
                finalDb.AddSubject(pair.subjects, pair.rank);
            }


            Debug.Log("B693: " + msw.CollectResults());
            return finalDb;
        }

        private Dictionary<VegetationLevelRank, IVegetationBiomesMap>
            CreateBiomesMaps(List<VegetationLevelRank> ranks,
                MyRectangle generationDistrict,
                PerPrecisionConfiguration perPrecisionConfiguration)
        {
            var biomeProvidersGenerators = ranks.Select(c => new
            {
                rank = c,
                generator = _biomeProvidersGenerators[c]
            });

            var gsw = MyStopWatch.RetriveGlobalStopWatch("BiomesMap");
            gsw.StartSegment("CreatieBiomesTree");

            var biomeProvidersTrees = biomeProvidersGenerators.Select(c => new
            {
                rank = c.rank,
                trees = c.generator.CreateBiomesTree(generationDistrict).Result
            });


            gsw.StartSegment("CreateBiomesArrayGenerator");

            var biomesArrayGenerators = biomeProvidersTrees.Select(c => new
            {
                c.rank,
                arrayGenerator = new VegetationBiomesArrayGenerator(
                    new HeightArrayDb(_terrainShapeDbProxy, _commonExecutor),
                    c.trees,
                    new RandomFieldFigureGeneratorUTProxy(new Ring2RandomFieldFigureGenerator(
                        new TextureRenderer(), new Ring2RandomFieldFigureGeneratorConfiguration()
                        {
                            PixelsPerUnit = new Vector2(perPrecisionConfiguration.BiomeArrayRandomPixelsPerUnit,
                                perPrecisionConfiguration.BiomeArrayRandomPixelsPerUnit)
                        }
                    )),
                    new VegetationBiomesArrayGenerator.VegetationBiomesArrayGeneratorConfiguration()
                    {
                        RandomHeightJitterRange = perPrecisionConfiguration.BiomeArrayHeightJitterRange
                    }
                )
            });

            var terrainDetailResolution = perPrecisionConfiguration.BiomeArrayGenerationTerrainResolution;

            gsw.StartSegment("Generate");
            var biomesMaps = biomesArrayGenerators.ToDictionary(c => c.rank,
                c => (IVegetationBiomesMap) c.arrayGenerator.Generate(
                    terrainDetailResolution,
                    RectangleUtils.CalculateTextureSize(generationDistrict.Size,
                        perPrecisionConfiguration.BiomeArrayPixelsPerUnit),
                    generationDistrict));
            gsw.StopSegment();
            return biomesMaps;
        }

        private Texture2D GenerateIntensityTexture(
            MyRectangle generationArea,
            PerPrecisionConfiguration perPrecisionConfiguration) //todo more complicated! Maybe class...
        {
            var material = new Material(Shader.Find("Custom/Misc/FbmNoiseGenerator"));
            material.SetFloat("_Scale", 3.5f);
            material.SetVector("_Coords", generationArea.ToVector4());
            material.SetVector("_OutValuesRange", perPrecisionConfiguration.IntensityMapPerPrecisionRange);

            var textureSize =
                RectangleUtils.CalculateTextureSize(generationArea.Size,
                    perPrecisionConfiguration.IntensityPixelsPerUnit);
            var renderTextureInfo = new RenderTextureInfo(textureSize.X, textureSize.Y, RenderTextureFormat.ARGB32,
                false);
            var conventionalTextureInfo =
                new ConventionalTextureInfo(textureSize.X, textureSize.Y, TextureFormat.ARGB32, false);

            var intensityTex =
                UltraTextureRenderer.RenderTextureAtOnce(material, renderTextureInfo, conventionalTextureInfo);

            //var intensityTex = new Texture2D(256, 256, TextureFormat.ARGB32, false);
            //for (int x = 0; x < 255; x++)
            //{
            //    for (int y = 0; y < 255; y++)
            //    {
            //        var intensity = x / 255f;
            //        intensityTex.SetPixel(x,y, new Color(intensity, intensity, intensity, intensity));
            //    }
            //}
            //intensityTex.Apply();
            //SavingFileManager.SaveTextureToPngFile($@"C:\inz2\treePlacingIntensity.png", intensityTex);
            return intensityTex;
        }
    }

    public class GeneralMultiDistrictPlacerConfiguration
    {
        public Vector2 GenerationDistrictSize;
        public Dictionary<VegetationPlacementPrecision, float> PrecisionsPerDistance;

        public Dictionary<VegetationPlacementPrecision, PerPrecisionConfiguration>
            PerPrecisionConfigurations;
    }

    public class PerPrecisionConfiguration
    {
        public List<VegetationLevelRank> UsedVegetationRanks;
        public VegetationPlacerConfiguration VegetationPlacerConfiguration;
        public float IntensityPixelsPerUnit;

        public float BiomeArrayRandomPixelsPerUnit;
        public Vector2 BiomeArrayHeightJitterRange = new Vector2(0.9f, 1.1f);
        public float BiomeArrayPixelsPerUnit;

        public TerrainCardinalResolution BiomeArrayGenerationTerrainResolution;
        public Vector2 IntensityMapPerPrecisionRange = new Vector2(0, 1);
    }


    public class BiomeProvidersFromHabitatGenerator
    {
        private HabitatMapDbProxy _habitatMapDb;
        private Dictionary<HabitatType, List<PrioritisedBiomeProvider>> _habitatsToBiomeProvidersDict;

        public BiomeProvidersFromHabitatGenerator(HabitatMapDbProxy habitatMapDb,
            Dictionary<HabitatType, List<PrioritisedBiomeProvider>> habitatsToBiomeProvidersDict)
        {
            _habitatMapDb = habitatMapDb;
            _habitatsToBiomeProvidersDict = habitatsToBiomeProvidersDict;
        }

        public async Task<MyQuadtree<BiomeProviderAtArea>> CreateBiomesTree(MyRectangle generationArea)
        {
            var outTree = new MyQuadtree<BiomeProviderAtArea>();
            var habitats = (await _habitatMapDb.Query(generationArea)).QueryAll().Select(c => c.Field).ToList();

            foreach (var habitatField in habitats)
            {
                Preconditions.Assert(_habitatsToBiomeProvidersDict.ContainsKey(habitatField.Type),
                    "No biomes for habitat type " + habitatField.Type);
                var biomeProviders = _habitatsToBiomeProvidersDict[habitatField.Type];
                foreach (var providerWithPriority in biomeProviders)
                {
                    outTree.Add(new BiomeProviderAtArea(habitatField.Geometry, providerWithPriority.BiomeProvider,
                        providerWithPriority.Priority));
                }
            }

            return outTree;
        }
    }

    public class PrioritisedBiomeProvider
    {
        public ITerrainInfoDependentBiomeProvider BiomeProvider;
        public float Priority;
    }
}