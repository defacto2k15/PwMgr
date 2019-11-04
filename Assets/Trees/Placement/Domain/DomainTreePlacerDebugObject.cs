using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement.BiomesMap;
using Assets.Trees.Placement.Habitats;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using UnityEngine;

namespace Assets.Trees.Placement.Domain
{
    public class DomainTreePlacerDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            var tester = new DebugTerrainTester(ComputeShaderContainer);
            tester.Start(false, true);

            CommonExecutorUTProxy commonExecutor = new CommonExecutorUTProxy();
            HabitatMapDbProxy habitatMapDbProxy = new HabitatMapDbProxy(new HabitatMapDb(
                new HabitatMapDb.HabitatMapDbInitializationInfo()
                {
                    RootSerializationPath = @"C:\inz\habitating2\"
                }));


            FloraDomainDbProxy domainDb = CreateDomainDb();
            var biomeProvidersGenerators = CreateBiomeConfigurationsDict(domainDb)
                .ToDictionary(c => c.Key, c => new BiomeProvidersFromHabitatGenerator(habitatMapDbProxy, c.Value));

            var genralPlacer = new GeneralMultiDistrictPlacer(new GeneralMultiDistrictPlacerConfiguration()
            {
                GenerationDistrictSize = new Vector2(90, 90),
                PrecisionsPerDistance = new Dictionary<VegetationPlacementPrecision, float>()
                {
                    {VegetationPlacementPrecision.High, 0}, //todo
                    {VegetationPlacementPrecision.Medium, 0},
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

            var vegetationCreationCharacteristicses = HabitatTreePlacesDebugObject
                .CreateRankDependentSpeciesCharacteristics();

            var repositioner = Repositioner.Default;
            var generationCenter = repositioner.InvMove(new Vector2(90 * 4.5f, 90 * 8.5f));
            var generationArea = repositioner.InvMove(new MyRectangle(90 * 8, 90 * 8, 90 * 8, 90 * 8));
            generationArea = new MyRectangle(47520, 52560, 720, 720);
            generationCenter = generationArea.Center;

            MyStopWatch.AddGlobalStopWatch("BiomesMap");
            var database = genralPlacer.Generate(
                generationArea,
                vegetationCreationCharacteristicses,
                generationCenter);

            HabitatTreePlacesDebugObject.CreateDebugObjectsForSubjects(database);
        }

        private FloraDomainDbProxy CreateDomainDb()
        {
            Dictionary<HabitatAndZoneType, ISpatialDb<FloraDomainIntensityArea>> db =
                new Dictionary<HabitatAndZoneType, ISpatialDb<FloraDomainIntensityArea>>();

            Dictionary<HabitatAndZoneType, List<FloraDomainCreationTemplate>> creationTemplates =
                new Dictionary<HabitatAndZoneType, List<FloraDomainCreationTemplate>>()
                {
                    {
                        new HabitatAndZoneType()
                        {
                            HabitatType = HabitatType.Fell,
                            ZoneType = HeightZoneType.High
                        },
                        new List<FloraDomainCreationTemplate>()
                        {
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 1f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain1
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.7f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain2
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.6f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain3
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.6f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain4
                            },
                        }
                    },
                    {
                        new HabitatAndZoneType()
                        {
                            HabitatType = HabitatType.Forest,
                            ZoneType = HeightZoneType.High
                        },
                        new List<FloraDomainCreationTemplate>()
                        {
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 1f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain1
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.7f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain2
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.6f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain3
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.6f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain4
                            },
                        }
                    },
                    {
                        new HabitatAndZoneType()
                        {
                            HabitatType = HabitatType.NotSpecified,
                            ZoneType = HeightZoneType.High
                        },
                        new List<FloraDomainCreationTemplate>()
                        {
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 1f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain1
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.7f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain2
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.6f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain3
                            },
                            new FloraDomainCreationTemplate()
                            {
                                MaxIntensity = 0.6f,
                                MinIntensity = 0.0f,
                                PositionMultiplier = 1f,
                                Type = FloraDomainType.Domain4
                            },
                        }
                    },
                };

            UnityThreadComputeShaderExecutorObject shaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            CommonExecutorUTProxy commonExecutor = new CommonExecutorUTProxy();
            FloraDomainIntensityGeneratorConfiguration configuration = new FloraDomainIntensityGeneratorConfiguration()
            {
                PixelsPerUnit = 1f / 3
            };

            SpatialDbConfiguration spatialConfiguration = new SpatialDbConfiguration()
            {
                QueryingCellSize = new Vector2(90 * 8, 90 * 8)
            };

            int i = 0;
            foreach (var pair in creationTemplates)
            {
                db.Add(pair.Key,
                    new CacheingSpatialDb<FloraDomainIntensityArea>(
                        new SpatialDb<FloraDomainIntensityArea>(
                            new FloraDomainIntensityGenerator(pair.Value, ComputeShaderContainer,
                                shaderExecutorObject, commonExecutor, i + 1, configuration), spatialConfiguration),
                        spatialConfiguration));
                i++;
            }

            return new FloraDomainDbProxy(db);
        }

        public static Dictionary<VegetationLevelRank, Dictionary<HabitatType, List<PrioritisedBiomeProvider>>>
            CreateBiomeConfigurationsDict(FloraDomainDbProxy domainDb)
        {
            //float perM2 = 60; // to musi! być też per vegetation rank
            //var singleResolutionDict = DebugCreateSingleResolutionBiomesProvider(perM2);

            return new Dictionary<VegetationLevelRank, Dictionary<HabitatType, List<PrioritisedBiomeProvider>>>()
            {
                {VegetationLevelRank.Big, DebugCreateSingleResolutionBiomesProvider(5, domainDb)},
                {VegetationLevelRank.Medium, DebugCreateSingleResolutionBiomesProvider(15, domainDb)},
                {VegetationLevelRank.Small, DebugCreateSingleResolutionBiomesProvider(40, domainDb)},
            };
        }

        private static Dictionary<HabitatType, List<PrioritisedBiomeProvider>>
            DebugCreateSingleResolutionBiomesProvider(float perM2, FloraDomainDbProxy floraDomainDb)
        {
            var perDomainBiomeProviders = new Dictionary<FloraDomainType, VegetationBiomeLevel>()
            {
                {
                    FloraDomainType.Domain1,
                    new VegetationBiomeLevel(
                        biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                        {
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1A,
                                0.7f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2A,
                                0.15f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3A,
                                0.15f),
                        },
                        exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                        maxElementsPerCellPerM2: perM2 / 100f)
                },
                {
                    FloraDomainType.Domain2,
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
                        maxElementsPerCellPerM2: perM2 / 100f)
                },
                {
                    FloraDomainType.Domain3,
                    new VegetationBiomeLevel(
                        biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                        {
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1C,
                                0.7f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2C,
                                0.15f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3C,
                                0.15f),
                        },
                        exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                        maxElementsPerCellPerM2: perM2 / 100f)
                },
                {
                    FloraDomainType.Domain4,
                    new VegetationBiomeLevel(
                        biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                        {
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1D,
                                0.7f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2D,
                                0.15f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3D,
                                0.15f),
                        },
                        exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                        maxElementsPerCellPerM2: perM2 / 100f)
                },
            };


            Dictionary<HabitatType, List<PrioritisedBiomeProvider>> outDict =
                new Dictionary<HabitatType, List<PrioritisedBiomeProvider>>()
                {
                    {
                        HabitatType.Forest, new List<PrioritisedBiomeProvider>()
                        {
                            new PrioritisedBiomeProvider()
                            {
                                BiomeProvider = new DomainDependentBiomeProvider(floraDomainDb, perDomainBiomeProviders,
                                    HabitatType.Forest, HeightZoneType.High),
                                Priority = 1
                            }
                        }
                    },
                    {
                        HabitatType.NotSpecified, new List<PrioritisedBiomeProvider>()
                        {
                            new PrioritisedBiomeProvider()
                            {
                                BiomeProvider = new DomainDependentBiomeProvider(floraDomainDb, perDomainBiomeProviders,
                                    HabitatType.NotSpecified, HeightZoneType.High),
                                Priority = 1
                            }
                        }
                    },
                    {
                        HabitatType.Grassland, new List<PrioritisedBiomeProvider>()
                        {
                            new PrioritisedBiomeProvider()
                            {
                                BiomeProvider = new DomainDependentBiomeProvider(floraDomainDb, perDomainBiomeProviders,
                                    HabitatType.Grassland, HeightZoneType.High),
                                Priority = 1
                            }
                        }
                    },
                };
            return outDict;
        }
    }

    public enum HeightZoneType
    {
        Low,
        Mid,
        High
    }

    public class DomainDependentBiomeProvider : ITerrainInfoDependentBiomeProvider
    {
        private FloraDomainDbProxy _floraDomainDb;
        private Dictionary<FloraDomainType, VegetationBiomeLevel> _domainToBiomeLevel;
        private HabitatType _habitatType;
        private HeightZoneType _zoneType;

        public DomainDependentBiomeProvider(FloraDomainDbProxy floraDomainDb,
            Dictionary<FloraDomainType, VegetationBiomeLevel> domainToBiomeLevel, HabitatType habitatType,
            HeightZoneType zoneType)
        {
            _floraDomainDb = floraDomainDb;
            _domainToBiomeLevel = domainToBiomeLevel;
            _habitatType = habitatType;
            _zoneType = zoneType;
        }

        public async Task<List<BiomeLevelWithStrength>> RetriveBiomesAt(TerrainInfo info)
        {
            var flatPosition = info.FlatPosition;
            var domains = await _floraDomainDb.Query(flatPosition, _habitatType, _zoneType);
            return _domainToBiomeLevel.Select(c => new BiomeLevelWithStrength(c.Value, domains.Retrive(c.Key)))
                .ToList();
        }
    }

    public class StandardBiomeProvider : ITerrainInfoDependentBiomeProvider
    {
        private List<VegetationBiomeLevel> _vegetationBiomeLevels;

        public StandardBiomeProvider(List<VegetationBiomeLevel> vegetationBiomeLevels)
        {
            _vegetationBiomeLevels = vegetationBiomeLevels;
        }

        public Task<List<BiomeLevelWithStrength>> RetriveBiomesAt(TerrainInfo info)
        {
            return TaskUtils.MyFromResult(_vegetationBiomeLevels.Select(c => new BiomeLevelWithStrength(c, 1))
                .ToList());
        }
    }


    public class EnhancedHeightDependentBiomeProvider : ITerrainInfoDependentBiomeProvider
    {
        private readonly MarginedRange _range;
        private ITerrainInfoDependentBiomeProvider _innerProvider;

        public EnhancedHeightDependentBiomeProvider(MarginedRange range,
            ITerrainInfoDependentBiomeProvider innerProvider)
        {
            _range = range;
            _innerProvider = innerProvider;
        }


        public async Task<List<BiomeLevelWithStrength>> RetriveBiomesAt(TerrainInfo info)
        {
            var biomes = await _innerProvider.RetriveBiomesAt(info);
            var height = info.Height;
            var strength = _range.PresenceFactor(height);
            return biomes.Select(c => new BiomeLevelWithStrength(c.BiomeLevel, c.Strength * strength)).ToList();
        }
    }
}