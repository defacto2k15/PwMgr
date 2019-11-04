using System.Collections.Generic;
using Assets.Habitat;
using Assets.Heightmaps;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement;
using Assets.Trees.Placement.Domain;
using Assets.Trees.Placement.Habitats;
using Assets.Utils;
using UnityEngine;

namespace Assets.PreComputation.Configurations
{
    public class BiomeProvidersConfiguration
    {
        private HeightDenormalizer _heightDenormalizer;

        public BiomeProvidersConfiguration(HeightDenormalizer heightDenormalizer)
        {
            _heightDenormalizer = heightDenormalizer;
        }

        public Dictionary<HeightZoneType, HeightZoneRangeDetails> ZoneRangeDict => new
            Dictionary<HeightZoneType, HeightZoneRangeDetails>()
            {
                {HeightZoneType.Low, new HeightZoneRangeDetails(new Vector2(0, 1100), 100, _heightDenormalizer)},
                {HeightZoneType.Mid, new HeightZoneRangeDetails(new Vector2(1100, 1400), 100, _heightDenormalizer)},
                {HeightZoneType.High, new HeightZoneRangeDetails(new Vector2(1400, 10000), 100, _heightDenormalizer)},
            };

        public class HeightZoneRangeDetails
        {
            private Vector2 _rangeInMeters;
            private float _marginInMeters;
            private HeightDenormalizer _heightDenormalizer;

            public HeightZoneRangeDetails(Vector2 rangeInMeters, float marginInMeters,
                HeightDenormalizer heightDenormalizer)
            {
                _rangeInMeters = rangeInMeters;
                _marginInMeters = marginInMeters;
                _heightDenormalizer = heightDenormalizer;
            }

            public MarginedRange ConstructRange()
            {
                var range = new Vector2(_heightDenormalizer.Normalize(_rangeInMeters.x),
                    _heightDenormalizer.Normalize(_rangeInMeters.y));
                var margin = _heightDenormalizer.NormalizeLength(_marginInMeters);
                return new MarginedRange(range, margin);
            }
        }


        public Dictionary<HabitatType, List<PrioritisedBiomeProvider>>
            Big_CreateSingleResolutionBiomesProvider(FloraDomainDbProxy floraDomainDb)
        {
            var perM2 = 4;

            var lowZoneRange = ZoneRangeDict[HeightZoneType.Low].ConstructRange();
            var midZoneRange = ZoneRangeDict[HeightZoneType.Mid].ConstructRange();
            var lowMidZoneRange = new MarginedRange(new Vector2(lowZoneRange.BaseMin, midZoneRange.BaseMax),
                midZoneRange.Margin);
            var topZoneRange = ZoneRangeDict[HeightZoneType.High].ConstructRange();
            var fullRange = new MarginedRange(new Vector2(lowZoneRange.BaseMin, topZoneRange.BaseMax),
                lowZoneRange.Margin);

            return new Dictionary<HabitatType, List<PrioritisedBiomeProvider>>()
            {
                {
                    HabitatType.Forest, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: lowZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.BeechForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Beech,
                                                            0.9f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Fir,
                                                            0.05f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Spruce,
                                                            0.05f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 0.5f))
                                        },
                                        {
                                            FloraDomainType.BeechFirSpruceForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Beech,
                                                            0.33f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Fir,
                                                            0.33f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Spruce,
                                                            0.33f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 0.5f))
                                        },
                                        {
                                            FloraDomainType.SpruceOnlyForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Beech,
                                                            0.005f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Fir,
                                                            0.005f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Spruce,
                                                            0.95f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 0.5f))
                                        },
                                        {
                                            FloraDomainType.LowClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Beech,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Fir,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Spruce,
                                                            0.6f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 5))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.Low)
                            )
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: midZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.SpruceOnlyForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Beech,
                                                            0.05f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Fir,
                                                            0.05f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Spruce,
                                                            0.9f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 0.5f))
                                        },
                                        {
                                            FloraDomainType.LowClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Beech,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Fir,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Spruce,
                                                            0.6f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 5))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.Mid))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: topZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.Pinus,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Pinus,
                                                            1.0f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 0.5f))
                                        },
                                        {
                                            FloraDomainType.HighClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Pinus,
                                                            1f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 5))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.High))
                        }
                    }
                },


                {
                    HabitatType.Fell, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Beech,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Fir,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Spruce,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 3))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.Meadow, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Beech,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Fir,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Spruce,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 6))
                                    }))
                        }
                    }
                },

                {
                    HabitatType.Grassland, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Beech,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Fir,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Spruce,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 7))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.Scrub, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: lowMidZoneRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Pinus,
                                                        0.95f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Spruce,
                                                        0.05f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 5))
                                    }))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: topZoneRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Pinus,
                                                        1.00f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (40f))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.NotSpecified, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: lowMidZoneRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Beech,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Fir,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Spruce,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 5))
                                    }))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: topZoneRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.Pinus,
                                                        1.00f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (40f))
                                    }))
                        }
                    }
                },
            };
        }

        public Dictionary<HabitatType, List<PrioritisedBiomeProvider>>
            Mid_CreateSingleResolutionBiomesProvider(FloraDomainDbProxy floraDomainDb)
        {
            var perM2 = 15;
            var lowZoneRange = ZoneRangeDict[HeightZoneType.Low].ConstructRange();
            var midZoneRange = ZoneRangeDict[HeightZoneType.Mid].ConstructRange();
            var lowMidZoneRange = new MarginedRange(new Vector2(lowZoneRange.BaseMin, midZoneRange.BaseMax),
                midZoneRange.Margin);
            var topZoneRange = ZoneRangeDict[HeightZoneType.High].ConstructRange();
            var fullRange = new MarginedRange(new Vector2(lowZoneRange.BaseMin, topZoneRange.BaseMax),
                lowZoneRange.Margin);

            return new Dictionary<HabitatType, List<PrioritisedBiomeProvider>>()
            {
                {
                    HabitatType.Forest, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: lowZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.BeechForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.BeechSmall,
                                                            0.8f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.FirSmall,
                                                            0.1f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SpruceSmall,
                                                            0.1f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 2.5f))
                                        },
                                        {
                                            FloraDomainType.BeechFirSpruceForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.BeechSmall,
                                                            0.33f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.FirSmall,
                                                            0.33f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SpruceSmall,
                                                            0.33f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 2.5f))
                                        },
                                        {
                                            FloraDomainType.SpruceOnlyForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.BeechSmall,
                                                            0.05f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.FirSmall,
                                                            0.05f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SpruceSmall,
                                                            0.9f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 2.5f))
                                        },
                                        {
                                            FloraDomainType.LowClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.BeechSmall,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.FirSmall,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SpruceSmall,
                                                            0.6f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 5))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.Low))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: midZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.SpruceOnlyForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.BeechSmall,
                                                            0.05f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.FirSmall,
                                                            0.05f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SpruceSmall,
                                                            0.9f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / 100f)
                                        },
                                        {
                                            FloraDomainType.LowClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.BeechSmall,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.FirSmall,
                                                            0.2f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SpruceSmall,
                                                            0.6f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 5))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.Mid))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: topZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.Pinus,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Pinus,
                                                            1.0f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 0.5f))
                                        },
                                        {
                                            FloraDomainType.HighClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.Pinus,
                                                            1f),
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 15))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.High))
                        }
                    }
                },


                {
                    HabitatType.Fell, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.BeechSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.FirSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SpruceSmall,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 8.0f))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.Meadow, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.BeechSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.FirSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SpruceSmall,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 10))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.Grassland, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.BeechSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.FirSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SpruceSmall,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 10))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.Scrub, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.BeechSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.FirSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SpruceSmall,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 5))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.NotSpecified, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: lowMidZoneRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.BeechSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.FirSmall,
                                                        0.33f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SpruceSmall,
                                                        0.33f),
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 10))
                                    }))
                        },
                        //new PrioritisedBiomeProvider()
                        //{
                        //    Priority = 1,
                        //    BiomeProvider = new EnhancingHeightDependentBiomeProvider(
                        //        range: topZoneRange,
                        //        innerProvider: new StandardBiomeProvider(
                        //            new List<VegetationBiomeLevel>()
                        //            {
                        //                new VegetationBiomeLevel(
                        //                    biomeOccurencePropabilities: new
                        //                        List<VegetationSpeciesOccurencePropability>()
                        //                        {
                        //                            new VegetationSpeciesOccurencePropability(
                        //                                VegetationSpeciesEnum.Pinus,
                        //                                1.00f),

                        //                        },
                        //                    exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                        //                    maxElementsPerCellPerM2: perM2 / (100f * 10))
                        //            }))
                        //}
                    }
                },
            };
        }


        public Dictionary<HabitatType, List<PrioritisedBiomeProvider>>
            Min_CreateSingleResolutionBiomesProvider(FloraDomainDbProxy floraDomainDb)
        {
            var perM2 = 25;

            var lowZoneRange = ZoneRangeDict[HeightZoneType.Low].ConstructRange();
            var midZoneRange = ZoneRangeDict[HeightZoneType.Mid].ConstructRange();
            var lowMidZoneRange = new MarginedRange(new Vector2(lowZoneRange.BaseMin, midZoneRange.BaseMax),
                midZoneRange.Margin);
            var topZoneRange = ZoneRangeDict[HeightZoneType.High].ConstructRange();
            var fullRange = new MarginedRange(new Vector2(lowZoneRange.BaseMin, topZoneRange.BaseMax),
                lowZoneRange.Margin);

            return new Dictionary<HabitatType, List<PrioritisedBiomeProvider>>()
            {
                {
                    HabitatType.Forest, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: lowZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.BeechForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / 100f)
                                        },
                                        {
                                            FloraDomainType.BeechFirSpruceForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / 100f)
                                        },
                                        {
                                            FloraDomainType.SpruceOnlyForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / 100f)
                                        },
                                        {
                                            FloraDomainType.LowClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / 100f)
                                        },
                                    }, HabitatType.Forest, HeightZoneType.Low))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: midZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.SpruceOnlyForrest,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / 100f)
                                        },
                                        {
                                            FloraDomainType.LowClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 5))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.Mid))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: topZoneRange,
                                innerProvider: new DomainDependentBiomeProvider(floraDomainDb,
                                    new Dictionary<FloraDomainType, VegetationBiomeLevel>()
                                    {
                                        {
                                            FloraDomainType.Pinus,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 0.5f))
                                        },
                                        {
                                            FloraDomainType.HighClearing,
                                            new VegetationBiomeLevel(
                                                biomeOccurencePropabilities: new
                                                    List<VegetationSpeciesOccurencePropability>()
                                                    {
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush1,
                                                            0.5f),
                                                        new VegetationSpeciesOccurencePropability(
                                                            VegetationSpeciesEnum.SmallBush2,
                                                            0.5f)
                                                    },
                                                exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                                maxElementsPerCellPerM2: perM2 / (100f * 15))
                                        },
                                    }, HabitatType.Forest, HeightZoneType.High))
                        }
                    }
                },


                {
                    HabitatType.Fell, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush1,
                                                        0.5f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush2,
                                                        0.5f)
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 5))
                                    }))
                        }
                    }
                },

                {
                    HabitatType.Grassland, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush1,
                                                        0.5f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush2,
                                                        0.5f)
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 10))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.Meadow, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush1,
                                                        0.5f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush2,
                                                        0.5f)
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 10))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.Scrub, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: fullRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush1,
                                                        0.5f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush2,
                                                        0.5f)
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 1))
                                    }))
                        }
                    }
                },
                {
                    HabitatType.NotSpecified, new List<PrioritisedBiomeProvider>()
                    {
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: lowMidZoneRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush1,
                                                        0.5f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush2,
                                                        0.5f)
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 10))
                                    }))
                        },
                        new PrioritisedBiomeProvider()
                        {
                            Priority = 1,
                            BiomeProvider = new EnhancedHeightDependentBiomeProvider(
                                range: topZoneRange,
                                innerProvider: new StandardBiomeProvider(
                                    new List<VegetationBiomeLevel>()
                                    {
                                        new VegetationBiomeLevel(
                                            biomeOccurencePropabilities: new
                                                List<VegetationSpeciesOccurencePropability>()
                                                {
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush1,
                                                        0.5f),
                                                    new VegetationSpeciesOccurencePropability(
                                                        VegetationSpeciesEnum.SmallBush2,
                                                        0.5f)
                                                },
                                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                                            maxElementsPerCellPerM2: perM2 / (100f * 10))
                                    }))
                        }
                    }
                },
            };
        }
    }
}