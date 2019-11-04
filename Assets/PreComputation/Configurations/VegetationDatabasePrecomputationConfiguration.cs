using System.Collections.Generic;
using Assets.Habitat;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement;
using Assets.Trees.Placement.Domain;
using Assets.Trees.Placement.Habitats;
using Assets.Utils;
using Assets.Utils.Spatial;
using UnityEngine;

namespace Assets.PreComputation.Configurations
{
    public class VegetationDatabasePrecomputationConfiguration
    {
        private Repositioner _repositioner;
        private BiomeProvidersConfiguration _biomeProvidersConfiguration;

        public VegetationDatabasePrecomputationConfiguration(Repositioner repositioner,
            HeightDenormalizer heightDenormalizer)
        {
            _repositioner = repositioner;
            _biomeProvidersConfiguration = new BiomeProvidersConfiguration(heightDenormalizer);
        }

        public MyRectangle GenerationArea =>
            _repositioner.InvMove(
                MyRectangle.FromVertex(new Vector2(-3600, -3240), new Vector2(3600, 2520)));
        //_repositioner.InvMove(UnityCoordsPositions2D.FromVertex(new Vector2(270, 0),  new Vector2(720, 360)));
        //_repositioner.InvMove( new UnityCoordsPositions2D(0, 90 * 4, 90 * 8, 90 * 8));

        //public UnityCoordsPositions2D GenerationArea => _repositioner.InvMove(new UnityCoordsPositions2D(0,-90*16, 90*16*2, 90*16*2));
        public Vector2 GenerationCenter => _repositioner.InvMove(new Vector2(575, -115));

        public Dictionary<VegetationLevelRank, Dictionary<HabitatType, List<PrioritisedBiomeProvider>>>
            BiomeConfigurationsDict(FloraDomainDbProxy floraDomainDb)
        {
            return new Dictionary<VegetationLevelRank, Dictionary<HabitatType, List<PrioritisedBiomeProvider>>>()
            {
                {
                    VegetationLevelRank.Big,
                    _biomeProvidersConfiguration.Big_CreateSingleResolutionBiomesProvider(floraDomainDb)
                },
                {
                    VegetationLevelRank.Medium,
                    _biomeProvidersConfiguration.Mid_CreateSingleResolutionBiomesProvider(floraDomainDb)
                },
                {
                    VegetationLevelRank.Small,
                    _biomeProvidersConfiguration.Min_CreateSingleResolutionBiomesProvider(floraDomainDb)
                },
            };
        }


        public GeneralMultiDistrictPlacerConfiguration GeneralMultiDistrictPlacerConfiguration => new
            GeneralMultiDistrictPlacerConfiguration
            {
                GenerationDistrictSize = new Vector2(90, 90),
                PrecisionsPerDistance = new Dictionary<VegetationPlacementPrecision, float>()
                {
                    {VegetationPlacementPrecision.High, 90 * 4}, //todo
                    {VegetationPlacementPrecision.Medium, 90 * 8},
                    {VegetationPlacementPrecision.Low, 30000}
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
                            BiomeArrayGenerationTerrainResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                            IntensityMapPerPrecisionRange = new Vector2(0.4f, 0.8f)
                        }
                    },
                    {
                        VegetationPlacementPrecision.Medium, new PerPrecisionConfiguration()
                        {
                            BiomeArrayHeightJitterRange = new Vector2(0.8f, 1.2f),
                            BiomeArrayPixelsPerUnit = 0.333f / 3,
                            BiomeArrayRandomPixelsPerUnit = 0.333f / 3,
                            UsedVegetationRanks = VegetationLevelRank.Medium.RetriveSameOrBigger(),
                            IntensityPixelsPerUnit = 0.333f / 3,
                            VegetationPlacerConfiguration = new VegetationPlacerConfiguration(),
                            BiomeArrayGenerationTerrainResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                            IntensityMapPerPrecisionRange = new Vector2(0.2f, 0.5f)
                        }
                    },
                    {
                        VegetationPlacementPrecision.Low, new PerPrecisionConfiguration()
                        {
                            BiomeArrayHeightJitterRange = new Vector2(0.7f, 1.3f),
                            BiomeArrayPixelsPerUnit = 0.333f / 9,
                            BiomeArrayRandomPixelsPerUnit = 0.333f / 9,
                            UsedVegetationRanks = VegetationLevelRank.Big.RetriveSameOrBigger(),
                            IntensityPixelsPerUnit = 0.333f / 9,
                            VegetationPlacerConfiguration = new VegetationPlacerConfiguration(),
                            BiomeArrayGenerationTerrainResolution = TerrainCardinalResolution.MIN_RESOLUTION,
                            IntensityMapPerPrecisionRange = new Vector2(0.1f, 0.4f)
                        }
                    },
                }
            };

        public Dictionary<VegetationLevelRank, List<VegetationSpeciesCreationCharacteristics>>
            RankDependentSpeciesCharacteristics()
        {
            return new Dictionary<VegetationLevelRank, List<VegetationSpeciesCreationCharacteristics>>()
            {
                {
                    VegetationLevelRank.Big,

                    new List<VegetationSpeciesCreationCharacteristics>()
                    {
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.Beech,
                            ExclusionRadius = 1f,
                            MaxCreationRadius = 5,
                            MaxSpeciesOccurenceDistance = 5f,
                            InitialSpeciesOccurencePropability = 0.4f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * 1,
                            //SpotDependentConcievingPropability todo start it
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                        },
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.Fir,
                            ExclusionRadius = 1f,
                            MaxCreationRadius = 5,
                            MaxSpeciesOccurenceDistance = 5f,
                            InitialSpeciesOccurencePropability = 0.4f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * 1,
                            //SpotDependentConcievingPropability 
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                        },
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.Spruce,
                            ExclusionRadius = 1f,
                            MaxCreationRadius = 5,
                            MaxSpeciesOccurenceDistance = 5f,
                            InitialSpeciesOccurencePropability = 0.8f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * 1,
                            //SpotDependentConcievingPropability 
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.95f), 0.1f))
                        },
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.Pinus,
                            ExclusionRadius = 0.5f,
                            MaxCreationRadius = 3,
                            MaxSpeciesOccurenceDistance = 3f,
                            InitialSpeciesOccurencePropability = 0.8f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * 1,
                            //SpotDependentConcievingPropability 
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.95f), 0.1f))
                        },
                    }
                },
                {
                    VegetationLevelRank.Medium,

                    new List<VegetationSpeciesCreationCharacteristics>()
                    {
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.BeechSmall,
                            ExclusionRadius = 1f * 0.6f,
                            MaxCreationRadius = 5,
                            MaxSpeciesOccurenceDistance = 5f,
                            InitialSpeciesOccurencePropability = 0.4f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f),
                            //SpotDependentConcievingPropability todo start it
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                        },
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.FirSmall,
                            ExclusionRadius = 1f * 0.6f,
                            MaxCreationRadius = 5,
                            MaxSpeciesOccurenceDistance = 5f,
                            InitialSpeciesOccurencePropability = 0.4f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f),
                            //SpotDependentConcievingPropability 
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                        },
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.SpruceSmall,
                            ExclusionRadius = 1f * 0.6f,
                            MaxCreationRadius = 5,
                            MaxSpeciesOccurenceDistance = 5f,
                            InitialSpeciesOccurencePropability = 0.8f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f),
                            //SpotDependentConcievingPropability 
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.95f), 0.1f))
                        },
                    }
                },
                {
                    VegetationLevelRank.Small,

                    new List<VegetationSpeciesCreationCharacteristics>()
                    {
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.SmallBush1,
                            ExclusionRadius = 1f * 0.3f,
                            MaxCreationRadius = 3,
                            MaxSpeciesOccurenceDistance = 3f,
                            InitialSpeciesOccurencePropability = 0.4f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                            //SpotDependentConcievingPropability todo start it
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                        },
                        new VegetationSpeciesCreationCharacteristics()
                        {
                            CurrentVegetationType = VegetationSpeciesEnum.SmallBush2,
                            ExclusionRadius = 1f * 0.3f,
                            MaxCreationRadius = 3,
                            MaxSpeciesOccurenceDistance = 3f,
                            InitialSpeciesOccurencePropability = 0.4f,
                            SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * 0.6f,
                            //SpotDependentConcievingPropability 
                            //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                        },
                    }
                }
            };
        }


        public VegetationOnRoadRemoverConfiguration VegetationOnRoadRemoverConfiguration =
            new VegetationOnRoadRemoverConfiguration()
            {
                RemovalCellSize = new Vector2(30, 30),
                PerDistanceRemovalPropabilities = new Dictionary<VegetationLevelRank, MyRange>()
                {
                    {VegetationLevelRank.Small, new MyRange(4, 8)},
                    {VegetationLevelRank.Medium, new MyRange(4, 8)},
                    {VegetationLevelRank.Big, new MyRange(4, 8)},
                }
            };

        public Dictionary<HabitatAndZoneType, List<FloraDomainCreationTemplate>> FloraDomainCreationTemplates =
            new Dictionary<HabitatAndZoneType, List<FloraDomainCreationTemplate>>()
            {
                {
                    new HabitatAndZoneType()
                    {
                        HabitatType = HabitatType.Forest,
                        ZoneType = HeightZoneType.Low
                    },
                    new List<FloraDomainCreationTemplate>()
                    {
                        new FloraDomainCreationTemplate()
                        {
                            Type = FloraDomainType.BeechFirSpruceForrest,
                            MaxIntensity = 1f,
                            MinIntensity = 0f,
                            PositionMultiplier = 1f
                        },
                        new FloraDomainCreationTemplate()
                        {
                            Type = FloraDomainType.BeechForrest,
                            MaxIntensity = 0.9f,
                            MinIntensity = 0f,
                            PositionMultiplier = 1f / 8
                        },
                        new FloraDomainCreationTemplate()
                        {
                            Type = FloraDomainType.SpruceOnlyForrest,
                            MaxIntensity = 0.8f,
                            MinIntensity = 0f,
                            PositionMultiplier = 1f
                        },
                        new FloraDomainCreationTemplate()
                        {
                            Type = FloraDomainType.LowClearing,
                            MaxIntensity = 0.65f,
                            MinIntensity = 0f,
                            PositionMultiplier = 0.7f
                        },
                    }
                },
                {
                    new HabitatAndZoneType()
                    {
                        HabitatType = HabitatType.Forest,
                        ZoneType = HeightZoneType.Mid
                    },
                    new List<FloraDomainCreationTemplate>()
                    {
                        new FloraDomainCreationTemplate()
                        {
                            Type = FloraDomainType.SpruceOnlyForrest,
                            MaxIntensity = 1f,
                            MinIntensity = 0f,
                            PositionMultiplier = 1f
                        },
                        new FloraDomainCreationTemplate()
                        {
                            Type = FloraDomainType.LowClearing,
                            MaxIntensity = 0.73f,
                            MinIntensity = 0f,
                            PositionMultiplier = 0.7f
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
                            Type = FloraDomainType.Pinus,
                            MaxIntensity = 1f,
                            MinIntensity = 0f,
                            PositionMultiplier = 1f
                        },
                        new FloraDomainCreationTemplate()
                        {
                            Type = FloraDomainType.HighClearing,
                            MaxIntensity = 0.8f,
                            MinIntensity = 0f,
                            PositionMultiplier = 0.7f
                        },
                    }
                },
            };

        public FloraDomainIntensityGeneratorConfiguration FloraConfiguration =
            new FloraDomainIntensityGeneratorConfiguration()
            {
                PixelsPerUnit = 1 / 9f
            };

        public SpatialDbConfiguration FloraDomainSpatialDbConfiguration = new SpatialDbConfiguration()
        {
            QueryingCellSize = new Vector2(90 * 8, 90 * 8)
        };
    }
}