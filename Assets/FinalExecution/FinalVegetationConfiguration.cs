using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Grass2;
using Assets.Grass2.IntenstityDb;
using Assets.Grass2.Types;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.Management;
using Assets.Utils;
using Assets.Utils.Spatial;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.FinalExecution
{
    public enum VegetationMode
    {
        Legacy, EVegetation
    }

    [Serializable]
    public class FinalVegetationConfiguration
    {
        public FinalVegetationReferencedAssets ReferencedAssets;

        public VegetationMode Mode = VegetationMode.EVegetation;
        public bool GenerateTrees = true;
        public bool GenerateBigBushes = false;
        public bool GenerateGrass = false;
        public bool GenerateSmallBushes = false;

        public FEConfiguration FeConfiguration { get; set; }

        /////////////////// Shifting!
        public Dictionary<VegetationSpeciesEnum, RootSinglePlantShiftingConfiguration> ShiftingConfigurations => new
            Dictionary<VegetationSpeciesEnum, RootSinglePlantShiftingConfiguration>()
            {
                {
                    VegetationSpeciesEnum.Beech, new RootSinglePlantShiftingConfiguration()
                    {
                        ClanName = "beech",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.5f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines.Trees_Beech)
                                    }
                                },
                                {
                                    VegetationDetailLevel.REDUCED, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.5f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Beech)
                                    }
                                },
                                {
                                    VegetationDetailLevel.BILLBOARD, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.5f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Beech)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.BeechSmall, new RootSinglePlantShiftingConfiguration()
                    {
                        ClanName = "beechSmall",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.35f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Beech)
                                    }
                                },
                                {
                                    VegetationDetailLevel.REDUCED, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.35f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Beech)
                                    }
                                },
                                {
                                    VegetationDetailLevel.BILLBOARD, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(0.9f, 0.5f, 0.9f) * 0.35f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Beech)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.Spruce, new RootSinglePlantShiftingConfiguration()
                    {
                        ClanName = "spruce",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 1.2f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Spruce)
                                    }
                                },
                                {
                                    VegetationDetailLevel.REDUCED, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 1.2f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Spruce)
                                    }
                                },
                                {
                                    VegetationDetailLevel.BILLBOARD, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 1.2f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Spruce)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.SpruceSmall, new RootSinglePlantShiftingConfiguration()
                    {
                        ClanName = "spruceSmall",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.7f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Spruce)
                                    }
                                },
                                {
                                    VegetationDetailLevel.REDUCED, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.7f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Spruce)
                                    }
                                },
                                {
                                    VegetationDetailLevel.BILLBOARD, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.3f, 0.5f, 1.3f) * 0.6f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Spruce)
                                    }
                                },
                            }
                        }
                    }
                },

                {
                    VegetationSpeciesEnum.Fir, new RootSinglePlantShiftingConfiguration()
                    {
                        ClanName = "cypress",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 1.2f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Cypress)
                                    }
                                },
                                {
                                    VegetationDetailLevel.REDUCED, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 1.2f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Cypress)
                                    }
                                },
                                {
                                    VegetationDetailLevel.BILLBOARD, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 1.2f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Cypress)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.FirSmall, new RootSinglePlantShiftingConfiguration()
                    {
                        ClanName = "cypresSmall",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.9f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Cypress)
                                    }
                                },
                                {
                                    VegetationDetailLevel.REDUCED, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.9f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Cypress)
                                    }
                                },
                                {
                                    VegetationDetailLevel.BILLBOARD, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.3f, 0.5f, 1.3f) * 1.0f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Cypress)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.Pinus, new RootSinglePlantShiftingConfiguration()
                    {
                        ClanName = "pinus",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.7f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Pinus)
                                    }
                                },
                                {
                                    VegetationDetailLevel.REDUCED, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.2f, 0.5f, 1.2f) * 0.7f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Pinus)
                                    }
                                },
                                {
                                    VegetationDetailLevel.BILLBOARD, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(1.3f, 0.5f, 1.3f) * 3.75f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Pinus)
                                    }
                                },
                            }
                        }
                    }
                },

                {
                    VegetationSpeciesEnum.SmallBush1, new RootSinglePlantShiftingConfiguration()
                    {
                        LivePrefabPath = "bush/bush 1",
                        EVegetationPrefabName = "Mushrooms",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(2, 1, 2) * 0.15f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Bush1)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.SmallBush2, new RootSinglePlantShiftingConfiguration()
                    {
                        LivePrefabPath = "bush/bush 2",
                        EVegetationPrefabName = "Shrub_03",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(2, 1, 2) * 0.15f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Bush1)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.SmallBush3, new RootSinglePlantShiftingConfiguration()
                    {
                        LivePrefabPath = "bush/bush 3",
                        EVegetationPrefabName = "Shrub_18",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(2, 1, 2) * 0.15f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Bush1)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.SmallBush4, new RootSinglePlantShiftingConfiguration()
                    {
                        LivePrefabPath = "bush/bush 4",
                        EVegetationPrefabName = "Stone_3",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(2, 1, 2) * 0.15f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Bush2)
                                    }
                                },
                            }
                        }
                    }
                },
                {
                    VegetationSpeciesEnum.SmallBush5, new RootSinglePlantShiftingConfiguration()
                    {
                        LivePrefabPath = "bush/bush 5",
                        EVegetationPrefabName = "Stump_03",
                        PlantDetailProviderDisposition = new MainPlantDetailProviderDisposition()
                        {
                            PerDetailDispositions = new Dictionary<VegetationDetailLevel, SingleDetailDisposition>()
                            {
                                {
                                    VegetationDetailLevel.FULL, new SingleDetailDisposition()
                                    {
                                        SizeMultiplier = new Vector3(2, 1, 2) * 0.15f,
                                        ColorGroups =
                                            FeConfiguration.ColorsConfiguration.ColorPaletteFile.RetrivePack(ColorPaletteLines
                                                .Trees_Bush2)
                                    }
                                },
                            }
                        }
                    }
                },
            };


        public Dictionary<VegetationSpeciesEnum, List<VegetationSpeciesEnum>> SpeciesChangingList => new
            Dictionary<VegetationSpeciesEnum, List<VegetationSpeciesEnum>>()
            {
                {
                    VegetationSpeciesEnum.SmallBush1, new List<VegetationSpeciesEnum>()
                    {
                        VegetationSpeciesEnum.SmallBush1,
                        VegetationSpeciesEnum.SmallBush2,
                        VegetationSpeciesEnum.SmallBush3
                    }
                },
                {
                    VegetationSpeciesEnum.SmallBush2, new List<VegetationSpeciesEnum>()
                    {
                        VegetationSpeciesEnum.SmallBush4,
                        VegetationSpeciesEnum.SmallBush5
                    }
                },
            };

        public VegetationRuntimeManagementConfiguration BushObjectsVegetationRuntimeManagementConfiguration => new
            VegetationRuntimeManagementConfiguration()
            {
                DetailFieldsTemplate = new SingleSquareDetailFieldsTemplate(100, VegetationDetailLevel.FULL)
            };

        public List<VegetationSpeciesEnum> SupportedTreeSpecies =>
            new List<VegetationSpeciesEnum>()
            {
                VegetationSpeciesEnum.Beech,
                //VegetationSpeciesEnum.BeechSmall,
                VegetationSpeciesEnum.Fir,
                //VegetationSpeciesEnum.FirSmall,
                VegetationSpeciesEnum.Pinus,
                VegetationSpeciesEnum.Spruce,
                //VegetationSpeciesEnum.SpruceSmall,
            };

        public List<VegetationSpeciesEnum> SupportedBushSpecies => new List<VegetationSpeciesEnum>()
        {
            VegetationSpeciesEnum.SmallBush1,
            VegetationSpeciesEnum.SmallBush2,
            VegetationSpeciesEnum.SmallBush3,
            VegetationSpeciesEnum.SmallBush4,
            VegetationSpeciesEnum.SmallBush5
        };

        public List<VegetationSpeciesEnum> SupportedVegetationSpecies => SupportedTreeSpecies.Union(SupportedBushSpecies).ToList();

        public List<VegetationSpeciesEnum> SupportedLeadingBushSpecies = new List<VegetationSpeciesEnum>()
        {
            VegetationSpeciesEnum.SmallBush1,
            VegetationSpeciesEnum.SmallBush2
        };


        //// Standard vegetation
        public VegetationRuntimeManagementConfiguration Grass2VegetationRuntimeManagementConfiguration =
            new VegetationRuntimeManagementConfiguration()
            {
                DetailFieldsTemplate = new SingleSquareDetailFieldsTemplate(22.5f * 2, VegetationDetailLevel.FULL),
                UpdateMinDistance = 10
            };

        public GrassVegetationSubjectsPositionsGenerator.GrassVegetationSubjectsPositionsGeneratorConfiguration
            GrassVegetationSubjectsPositionsGeneratorConfiguration =
                new GrassVegetationSubjectsPositionsGenerator.GrassVegetationSubjectsPositionsGeneratorConfiguration()
                {
                    PositionsGridSize = new Vector2(11.25f, 11.25f)
                };

        public Grass2RuntimeManager.Grass2RuntimeManagerConfiguration Grass2RuntimeManagerConfiguration =
            new Grass2RuntimeManager.Grass2RuntimeManagerConfiguration()
            {
                GroupSize = new Vector2(11.25f, 11.25f)
            };


        public Dictionary<VegetationLevelRank, VegetationRuntimeManagementConfiguration>
            PerRankVegetationRuntimeManagementConfigurations => new
            Dictionary<VegetationLevelRank, VegetationRuntimeManagementConfiguration>()
            {
                {
                    VegetationLevelRank.Big,
                    new VegetationRuntimeManagementConfiguration()
                    {
                        DetailFieldsTemplate = new CenterHolesDetailFieldsTemplate(
                            new List<DetailFieldsTemplateOneLine>()
                            {
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.FULL, 0, 35),
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.REDUCED, 25, 70),
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.BILLBOARD, 60, 10000),
                            }),
                        UpdateMinDistance = 10
                    }
                },


                {
                    VegetationLevelRank.Medium, new VegetationRuntimeManagementConfiguration()
                    {
                        DetailFieldsTemplate = new CenterHolesDetailFieldsTemplate(
                            new List<DetailFieldsTemplateOneLine>()
                            {
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.FULL, 0, 30),
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.REDUCED, 25, 60),
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.BILLBOARD, 50, 280),
                            }),
                        UpdateMinDistance = 10
                    }
                },
                {
                    VegetationLevelRank.Small,
                    new VegetationRuntimeManagementConfiguration()
                    {
                        DetailFieldsTemplate = new CenterHolesDetailFieldsTemplate(
                            new List<DetailFieldsTemplateOneLine>()
                            {
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.FULL, 0, 25),
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.REDUCED, 20, 35),
                                new DetailFieldsTemplateOneLine(VegetationDetailLevel.BILLBOARD, 32, 280),
                            }),
                        UpdateMinDistance = 10
                    }
                }
            };

        public MyRectangle NonStagnantVegetationArea => FeConfiguration.Repositioner.InvMove(MyRectangle.CenteredAt(
            new Vector2(FeConfiguration.CameraStartPosition.x, FeConfiguration.CameraStartPosition.z), new Vector2(600, 600)));

        public StagnantVegetationRuntimeManagementConfiguration StagnantVegetationRuntimeManagementConfiguration =
            new StagnantVegetationRuntimeManagementConfiguration()
            {
                DetailLevel = VegetationDetailLevel.BILLBOARD
            };


        ////////////////// GRASS2
        public HabitatToGrassIntensityMapGenerator.HabitatToGrassIntensityMapGeneratorConfiguration
            HabitatToGrassIntensityMapGeneratorConfiguration =
                new HabitatToGrassIntensityMapGenerator.HabitatToGrassIntensityMapGeneratorConfiguration()
                {
                    GrassTypeToSourceHabitats = new Dictionary<GrassType, List<HabitatType>>()
                    {
                        {GrassType.Debug1, new List<HabitatType>() {HabitatType.Forest}},
                        {GrassType.Debug2, new List<HabitatType>() {HabitatType.Meadow, HabitatType.Fell}},
                    },
                    OutputPixelsPerUnit = 1
                };

        public HabitatTexturesGenerator.HabitatTexturesGeneratorConfiguration HabitatTexturesGeneratorConfiguration =
            new HabitatTexturesGenerator.HabitatTexturesGeneratorConfiguration()
            {
                HabitatMargin = 5,
                HabitatSamplingUnit = 3
            };

        public Grass2IntensityMapGenerator.Grass2IntensityMapGeneratorConfiguration
            Grass2IntensityMapGeneratorConfiguration =
                new Grass2IntensityMapGenerator.Grass2IntensityMapGeneratorConfiguration()
                {
                    HabitatSamplingUnit = 3
                };

        public SpatialDbConfiguration Grass2IntensityDbConfiguration = new SpatialDbConfiguration()
        {
            QueryingCellSize = new Vector2(22.5f, 22.5f)
        };

        public Dictionary<GrassType, GrassTypeTemplate> GrassTemplates => new Dictionary<GrassType, GrassTypeTemplate>()
        {
            {
                GrassType.Debug1, new GrassTypeTemplate()
                {
                    Color = new Triplet<RandomCharacteristics>()
                    {
                        A = new RandomCharacteristics()
                        {
                            Mean = 0.0f,
                            StandardDeviation = 0.05f,
                        },
                        B = new RandomCharacteristics()
                        {
                            Mean = 0.8f,
                            StandardDeviation = 0.0f,
                        },
                        C = new RandomCharacteristics()
                        {
                            Mean = 0.0f,
                            StandardDeviation = 0.0f,
                        }
                    },
                    InitialBendingValue = new RandomCharacteristics()
                    {
                        Mean = 0.8f,
                        StandardDeviation = 0.1f,
                    },
                    InitialBendingStiffness = new RandomCharacteristics()
                    {
                        Mean = 0.0f,
                        StandardDeviation = 0.1f,
                    },
                    FlatSize = new Pair<RandomCharacteristics>()
                    {
                        A = new RandomCharacteristics() //width
                        {
                            StandardDeviation = 0.005f,
                            Mean = 0.2f,
                        },
                        B = new RandomCharacteristics() //height
                        {
                            Mean = 1.7f / 8,
                            StandardDeviation = 0.1f
                        }
                    },
                    InstancesPerUnitSquare = 70
                }
            },
            {
                GrassType.Debug2, new GrassTypeTemplate()
                {
                    Color = new Triplet<RandomCharacteristics>()
                    {
                        A = new RandomCharacteristics()
                        {
                            Mean = 0.709f * 1.3f,
                            StandardDeviation = 0.02f,
                        },
                        B = new RandomCharacteristics()
                        {
                            Mean = 0.585f * 1.3f,
                            StandardDeviation = 0.02f,
                        },
                        C = new RandomCharacteristics()
                        {
                            Mean = 0.385f * 1.3f,
                            StandardDeviation = 0.02f,
                        }
                    },
                    InitialBendingValue = new RandomCharacteristics()
                    {
                        Mean = 0.0f,
                        StandardDeviation = 0.1f,
                    },
                    InitialBendingStiffness = new RandomCharacteristics()
                    {
                        Mean = 0.9f,
                        StandardDeviation = 0.1f,
                    },
                    FlatSize = new Pair<RandomCharacteristics>()
                    {
                        A = new RandomCharacteristics() //width
                        {
                            StandardDeviation = 0.03f,
                            Mean = 0.2f,
                        },
                        B = new RandomCharacteristics() //height
                        {
                            Mean = 0.7f,
                            StandardDeviation = 0.1f
                        }
                    },
                    InstancesPerUnitSquare = 70
                }
            },
        };




        /////////// Bush grass
        public Vector2 BushSingleGenerationArea = new Vector2(11.25f, 11.25f);

        public VegetationRuntimeManagementConfiguration BushVegetationRuntimeManagementConfiguration =
            new VegetationRuntimeManagementConfiguration()
            {
                DetailFieldsTemplate = new SingleSquareDetailFieldsTemplate(120, VegetationDetailLevel.FULL),
                UpdateMinDistance = 10
            };

        public MyRange BushExclusionRadiusRange = new MyRange(1.5f * 0.35f, 1.5f * 1f);

        public Dictionary<GrassType, GrassTypeTemplate> BushTemplatesConfiguration =>
            new Dictionary<GrassType, GrassTypeTemplate>()
            {
                {
                    GrassType.Debug1, new GrassTypeTemplate()
                    {
                        Color = new Triplet<RandomCharacteristics>()
                        {
                            A = new RandomCharacteristics()
                            {
                                Mean = 0.0f,
                                StandardDeviation = 0.05f,
                            },
                            B = new RandomCharacteristics()
                            {
                                Mean = 0.8f,
                                StandardDeviation = 0.0f,
                            },
                            C = new RandomCharacteristics()
                            {
                                Mean = 0.0f,
                                StandardDeviation = 0.0f,
                            }
                        },
                        InitialBendingValue = new RandomCharacteristics()
                        {
                            Mean = 0.0f,
                            StandardDeviation = 0.0f,
                        },
                        InitialBendingStiffness = new RandomCharacteristics()
                        {
                            Mean = 1.0f,
                            StandardDeviation = 0.1f,
                        },
                        FlatSize = new Pair<RandomCharacteristics>()
                        {
                            A = new RandomCharacteristics() //width
                            {
                                StandardDeviation = 0.4f,
                                Mean = 2.1f,
                            },
                            B = new RandomCharacteristics() //height
                            {
                                Mean = 3.1f,
                                StandardDeviation = 0.4f
                            }
                        },
                        InstancesPerUnitSquare = 1.5f
                    }
                },
                {
                    GrassType.Debug2, new GrassTypeTemplate()
                    {
                        Color = new Triplet<RandomCharacteristics>()
                        {
                            A = new RandomCharacteristics()
                            {
                                Mean = 0.809f,
                                StandardDeviation = 0.01f,
                            },
                            B = new RandomCharacteristics()
                            {
                                Mean = 0.665f,
                                StandardDeviation = 0.01f,
                            },
                            C = new RandomCharacteristics()
                            {
                                Mean = 0.435f,
                                StandardDeviation = 0.01f,
                            }
                        },
                        InitialBendingValue = new RandomCharacteristics()
                        {
                            Mean = 0.2f,
                            StandardDeviation = 0.0f,
                        },
                        InitialBendingStiffness = new RandomCharacteristics()
                        {
                            Mean = 0.9f,
                            StandardDeviation = 0.0f,
                        },
                        FlatSize = new Pair<RandomCharacteristics>()
                        {
                            A = new RandomCharacteristics() //width
                            {
                                Mean = 0.5f,
                                StandardDeviation = 0.2f,
                            },
                            B = new RandomCharacteristics() //height
                            {
                                Mean = 2f,
                                StandardDeviation = 0.4f
                            }
                        },
                        InstancesPerUnitSquare = 15
                    }
                },
            };

        /// ////////// Vegetation
        public string VegetationDatabaseFilePath => FeConfiguration.FilePathsConfiguration.VegetationDatabaseFilePath;

        public string LoadingVegetationDatabaseDictionaryPath => FeConfiguration.FilePathsConfiguration.LoadingVegetationDatabaseDictionaryPath;

        public string Grass2BillboardsPath => FeConfiguration.FilePathsConfiguration.Grass2BillboardsPath;
        public string TreeCompletedClanDirectiory => FeConfiguration.FilePathsConfiguration.TreeCompletedClanDirectiory;
        public ShadowCastingMode GrassCastShadows => ShadowCastingMode.Off;
    }


    [Serializable]
    public class FinalVegetationReferencedAssets
    {
        public Texture EVegetationMainTexture;
        public Material GrassMaterial;
        public List<GameObject> Prefabs;
    }

    public class RootSinglePlantShiftingConfiguration
    {
        public string ClanName;
        public string LivePrefabPath;
        public string EVegetationPrefabName;
        public MainPlantDetailProviderDisposition PlantDetailProviderDisposition;
    }
}