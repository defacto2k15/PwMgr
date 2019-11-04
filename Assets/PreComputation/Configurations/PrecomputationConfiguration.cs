using System.Collections.Generic;
using Assets.FinalExecution;
using Assets.Habitat;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using Assets.Repositioning;
using Assets.Roads.Pathfinding;
using Assets.TerrainMat;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using UnityEngine;

namespace Assets.PreComputation.Configurations
{
    public class PrecomputationConfiguration
    {
        private FEConfiguration _feConfiguration;
        private FilePathsConfiguration _filePathsConfiguration;

        public PrecomputationConfiguration(FEConfiguration feConfiguration,
            FilePathsConfiguration filePathsConfiguration)
        {
            _feConfiguration = feConfiguration;
            _filePathsConfiguration = filePathsConfiguration;
        }

        public bool Multithreading = false;
        public Repositioner Repositioner = Repositioner.Default;
        public HeightDenormalizer HeightDenormalizer = HeightDenormalizer.Default;

        public GeoCoordsToUnityTranslator GeoCoordsToUnityTranslator = GeoCoordsToUnityTranslator.DefaultTranslator;


        /////////////// Ring1/Stain precomputation
        public RoadToBiomesConventerConfiguration RoadToBiomesConventerConfiguration =
            new RoadToBiomesConventerConfiguration()
            {
                PathBiomePriority = 9999
            };


        public HabitatToStainBiomeConversionConfiguration HabitatToStainBiomeConversionConfiguration =
            new HabitatToStainBiomeConversionConfiguration()
            {
                ConversionSpecifications = new Dictionary<HabitatType, BiomeFromHabitatSpecification>()
                {
                    {
                        HabitatType.Forest, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Forest,
                            Priority = 1
                        }
                    },
                    {
                        HabitatType.NotSpecified, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.NotSpecified,
                            Priority = 1
                        }
                    },
                    {
                        HabitatType.Meadow, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Meadow,
                            Priority = 2
                        }
                    },
                    {
                        HabitatType.Grassland, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Grassland,
                            Priority = 3
                        }
                    },
                    {
                        HabitatType.Fell, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Fell,
                            Priority = 5
                        }
                    },
                    {
                        HabitatType.Scrub, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Scrub,
                            Priority = 5
                        }
                    },
                }
            };

        public StainTerrainProviderConfiguration StainTerrainProviderConfiguration =>
            new StainTerrainProviderConfiguration()
            {
                BiomesContainerConfiguration = new BiomesContainerConfiguration()
                {
                    HighQualityQueryDistance = 2000000.99f,
                    DefaultType = BiomeType.Grass
                },
                StainTerrainCoords =
                    _feConfiguration.Repositioner.InvMove(
                        MyRectangle.FromVertex(new Vector2(-3600, -3240), new Vector2(3600, 2520))),

                RoadDisplayingAreaNormalizedCoords =
                    MyRectangle.CenteredAt(new Vector2(0.5f, 0.5f), new Vector2(0.1f, 0.1f)),
                StainTerrainArrayFromBiomesGeneratorConfiguration =
                    new StainTerrainArrayFromBiomesGeneratorConfiguration()
                    {
                        TexturesSideLength = 256,
                        PriorityWeightFactor = 5
                    },
                BiomeGenerationTemplates = BiomeInstanceDetailTemplates
            };

        public FromManualTextureStainResourceLoaderConfiguration FromManualTextureStainResourceLoaderConfiguration =>
            new FromManualTextureStainResourceLoaderConfiguration()
            {
                ControlColorToBiomeTypeDict = new Dictionary<Color, BiomeType>()
                {
                    {
                        Color.yellow, BiomeType.Forest
                    },
                    {
                        Color.red, BiomeType.NotSpecified
                    },
                    {
                        Color.blue, BiomeType.Scrub
                    },
                    {
                        Color.green, BiomeType.Fell
                    },
                },
                InputTextureSize = new IntVector2(1024, 1024),
                ManualTexturePath = _filePathsConfiguration.ManualRing1TexturePath
            };


        public Dictionary<BiomeType, BiomeInstanceDetailTemplate> BiomeInstanceDetailTemplates =>
            new Dictionary<BiomeType, BiomeInstanceDetailTemplate>()
            {
                {
                    BiomeType.Forest, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    ColorUtils.FromHex("228b22"),
                                    ColorUtils.FromHex("90c590"),
                                    ColorUtils.FromHex("568b22"),
                                    ColorUtils.FromHex("134e13"),
                                })
                            },
                            DeltaCharacteristics = new RandomCharacteristics()
                            {
                                StandardDeviation = 0,
                                Mean = 0.1f
                            }
                        },
                        ControlTemplate = new BiomeControlTemplate()
                        {
                            BaseControlValues = new List<Vector4>()
                            {
                                new Vector4(0.5f, 0.5f, 0.5f, 0.5f)
                            },
                            DeltaControls = new RandomCharacteristics()
                            {
                                StandardDeviation = 0.1f,
                                Mean = 0
                            }
                        }
                    }
                },
                {
                    BiomeType.NotSpecified, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    ColorUtils.FromHex("113318"),
                                    ColorUtils.FromHex("1f3b1b"),
                                    ColorUtils.FromHex("168d3c"),
                                    ColorUtils.FromHex("199c6b")
                                })
                            },
                            DeltaCharacteristics = new RandomCharacteristics()
                            {
                                StandardDeviation = 0,
                                Mean = 0.1f
                            }
                        },
                        ControlTemplate = new BiomeControlTemplate()
                        {
                            BaseControlValues = new List<Vector4>()
                            {
                                new Vector4(0.5f, 0.5f, 0.5f, 0.5f)
                            },
                            DeltaControls = new RandomCharacteristics()
                            {
                                StandardDeviation = 0.3f,
                                Mean = 0
                            }
                        }
                    }
                },
                {
                    BiomeType.Scrub, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    ColorUtils.FromHex("01a611"),
                                    ColorUtils.FromHex("007b0c"),
                                    ColorUtils.FromHex("84af2c"),
                                    ColorUtils.FromHex("2caf58"),
                                })
                            },
                            DeltaCharacteristics = new RandomCharacteristics()
                            {
                                StandardDeviation = 0,
                                Mean = 0.1f
                            }
                        },
                        ControlTemplate = new BiomeControlTemplate()
                        {
                            BaseControlValues = new List<Vector4>()
                            {
                                new Vector4(0.5f, 0.5f, 0.5f, 0.5f)
                            },
                            DeltaControls = new RandomCharacteristics()
                            {
                                StandardDeviation = 0.3f,
                                Mean = 0
                            }
                        }
                    }
                },
                {
                    BiomeType.Grassland, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    ColorUtils.FromHex("01a611"),
                                    ColorUtils.FromHex("007b0c"),
                                    ColorUtils.FromHex("84af2c"),
                                    ColorUtils.FromHex("2caf58"),
                                })
                            },
                            DeltaCharacteristics = new RandomCharacteristics()
                            {
                                StandardDeviation = 0,
                                Mean = 0.1f
                            }
                        },
                        ControlTemplate = new BiomeControlTemplate()
                        {
                            BaseControlValues = new List<Vector4>()
                            {
                                new Vector4(0.5f, 0.5f, 0.5f, 0.5f)
                            },
                            DeltaControls = new RandomCharacteristics()
                            {
                                StandardDeviation = 0.3f,
                                Mean = 0
                            }
                        }
                    }
                },
                {
                    BiomeType.Meadow, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    new Color(0.2f, 0.2f, 0.2f),
                                    new Color(0.5f, 0.5f, 0.5f),
                                    new Color(0.7f, 0.7f, 0.7f),
                                    new Color(1, 1, 1),
                                })
                            },
                            DeltaCharacteristics = new RandomCharacteristics()
                            {
                                StandardDeviation = 0,
                                Mean = 0.1f
                            }
                        },
                        ControlTemplate = new BiomeControlTemplate()
                        {
                            BaseControlValues = new List<Vector4>()
                            {
                                new Vector4(0.5f, 0.5f, 0.5f, 0.5f)
                            },
                            DeltaControls = new RandomCharacteristics()
                            {
                                StandardDeviation = 0.3f,
                                Mean = 0
                            }
                        }
                    }
                },
                {
                    BiomeType.Fell, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    ColorUtils.FromHex("76c641"),
                                    ColorUtils.FromHex("318328"),
                                    ColorUtils.FromHex("236628"),
                                    ColorUtils.FromHex("5e9d34")
                                })
                            },
                            DeltaCharacteristics = new RandomCharacteristics()
                            {
                                StandardDeviation = 0,
                                Mean = 0.1f
                            }
                        },
                        ControlTemplate = new BiomeControlTemplate()
                        {
                            BaseControlValues = new List<Vector4>()
                            {
                                new Vector4(0.5f, 0.5f, 0.5f, 0.5f)
                            },
                            DeltaControls = new RandomCharacteristics()
                            {
                                StandardDeviation = 0.3f,
                                Mean = 0
                            }
                        }
                    }
                }
            };
    }
}