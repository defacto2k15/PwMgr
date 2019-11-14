using System;
using System.Collections.Generic;
using System.Text;
using Assets.Caching;
using Assets.Habitat;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.Stamping;
using Assets.Roads;
using Assets.Roads.Engraving;
using Assets.Roads.Pathfinding;
using Assets.Roads.TerrainFeature;
using Assets.Scheduling;
using Assets.Trees.DesignBodyDetails;
using Assets.Utils;
using Assets.Utils.Spatial;
using Assets.Utils.UTUpdating;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class FEConfiguration
    {
        private FilePathsConfiguration _filePathsConfiguration;
        private FinalColorsConfiguration _colorsConfiguration;

        public FEConfiguration(FilePathsConfiguration filePathsConfiguration)
        {
            _filePathsConfiguration = filePathsConfiguration;
            _colorsConfiguration = new FinalColorsConfiguration(new ColorPaletteFileManagerConfiguration()
            {
                FilePath = filePathsConfiguration.ColorPaletteFilePath,
                TextureSize = new IntVector2(32, 64),
                MaxOneLineColorCount = 8,
                EmptyColor = new Color(0, 0, 0, 0)
            });
        }

        public FinalColorsConfiguration ColorsConfiguration => _colorsConfiguration;

        public FilePathsConfiguration FilePathsConfiguration => _filePathsConfiguration;

        public bool Multithreading = false;

        public Repositioner Repositioner = Repositioner.Default;
        public HeightDenormalizer HeightDenormalizer = HeightDenormalizer.Default;

        public Vector3 CameraStartPosition
        {
            get
            {
                return new Vector3(499, 909, -21);
            }
        }

        public bool UseCameraFlightDemo = false;
        public bool UpdateRingTree = true;


        public string HabitatDbFilePath => _filePathsConfiguration.HabitatDbFilePath;

        public HabitatMapDb.HabitatMapDbInitializationInfo HabitatDbInitializationInfo =>
            new HabitatMapDb.HabitatMapDbInitializationInfo()
            {
                RootSerializationPath = HabitatDbFilePath
            };

        //TODO this is ugly. Configuration should not have functions
        public Ring2RegionsDbGeneratorConfiguration Ring2RegionsDbGeneratorConfiguration(
            Ring2AreaDistanceDatabase distanceDatabase)
        {
            return new Ring2RegionsDbGeneratorConfiguration()
            {
                FromHabitatTemplates = HabitatTemplatesCreator(distanceDatabase),
                FromPathsTemplate = FromPathsTemplate(distanceDatabase),
                PathWidth = 4,
                Ring2RoadsQueryArea = Repositioner.InvMove(new MyRectangle(0, 0, 1080, 1080)),
                GenerateRoadHabitats = true
            };
        }

        public Ring2RegionFromHabitatTemplate FromPathsTemplate(Ring2AreaDistanceDatabase distanceDatabase)
        {
            return new Ring2RegionFromHabitatTemplate()
            {
                Fabrics = new List<Ring2Fabric>()
                {
                    new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(
                            _colorsConfiguration.ColorPaletteFile.RetriveList(ColorPaletteLines.Road1, 4)),
                        new FromAreaEdgeDistanceRing2IntensityProvider(0.2f, distanceDatabase), 1,5),
                    new Ring2Fabric(Ring2Fiber.DottedTerrainFiber, new Ring2FabricColors(
                            _colorsConfiguration.ColorPaletteFile.RetriveList(ColorPaletteLines.Road1_Dots, 4)),
                        new FromAreaEdgeDistanceRing2IntensityProvider(0.4f, distanceDatabase), 0.8f, 5),
                },
                Magnitude = 10,
                BufferLength = 1
            };
        }

        public Dictionary<HabitatType, Ring2RegionFromHabitatTemplate> HabitatTemplatesCreator(
            Ring2AreaDistanceDatabase distanceDatabase)
        {
            return new Dictionary<HabitatType, Ring2RegionFromHabitatTemplate>()
            {
                {
                    HabitatType.Forest, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Forrest_Ground, 4))
                                , new FromAreaEdgeDistanceRing2IntensityProvider(5f, distanceDatabase), 0.3f, 1),
                            new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Forrest_GrassyField, 4))
                                , new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.6f, 1)
                        },
                        Magnitude = 1,
                        BufferLength = 5
                    }
                },
                {
                    HabitatType.Fell, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Fell_Ground, 4))
                                , new FromAreaEdgeDistanceRing2IntensityProvider(5f, distanceDatabase), 0.4f,1),
                            new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Fell_GrassyField, 4))
                                , new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.65f,1)
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
                            new Ring2Fabric(Ring2Fiber.DottedTerrainFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Not_Specified_Dotted, 4)),
                                new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.4f,1),
                            new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Not_Specified_Ground, 4)),
                                new FromAreaEdgeDistanceRing2IntensityProvider(5f, distanceDatabase), 0.3f,1f),
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
                            new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Grassland_GrassyField, 4)),
                                new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.7f,1),
                            new Ring2Fabric(Ring2Fiber.BaseGroundFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Grassland_Ground, 4)),
                                new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.3f,1)
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
                            new Ring2Fabric(Ring2Fiber.DrySandFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Meadow_DrySand, 4)),
                                new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.7f, 1)
                        },
                        Magnitude = 3,
                        BufferLength = 3
                    }
                },
                {
                    HabitatType.Scrub, new Ring2RegionFromHabitatTemplate()
                    {
                        Fabrics = new List<Ring2Fabric>()
                        {
                            new Ring2Fabric(Ring2Fiber.GrassyFieldFiber, new Ring2FabricColors(
                                    _colorsConfiguration.ColorPaletteFile.RetriveList(
                                        ColorPaletteLines.Ring2_Habitat_Meadow_DrySand, 4)),
                                new FromAreaEdgeDistanceRing2IntensityProvider(1.3f, distanceDatabase), 0.75f,1)
                        },
                        Magnitude = 3,
                        BufferLength = 3
                    }
                },
            };
        }


        ///////////////////////////RING2///////////////
        public Vector2 Ring2PatchSize = new Vector2(45, 45); // this is duplicated!

        public MyRectangle Ring2GenerationArea
        {
            get
            {
                var ring2GenerationArea =
                    new MyRectangle(46440f - 360 * 6, 51840f - 360 * 6, 360 * 12, 360 * 12);

                return Ring2RegionsFromHabitatDebugObject.AlignGenerationAreaToPatch(ring2GenerationArea,
                    Ring2PatchSize);
            }
        }


        ///////////// TextureRendererService
        public TextureRendererServiceConfiguration TextureRendererServiceConfiguration =>
            new TextureRendererServiceConfiguration()
            {
                StepSize = new Vector2(500, 500)
            };

        /////////////////////// ROADS DATABASE
        public string RoadDatabasePath => _filePathsConfiguration.PathsPath;

        public PathProximityArrayGenerator.PathProximityArrayGeneratorConfiguration
            PathProximityArrayGeneratorConfiguration =>
            new PathProximityArrayGenerator.PathProximityArrayGeneratorConfiguration() { };

        public PathProximityTextureGenerator.PathProximityTextureGeneratorConfiguration
            PathProximityTextureGeneratorConfiguration =>
            new PathProximityTextureGenerator.PathProximityTextureGeneratorConfiguration();

        public PathProximityTextureProviderConfiguration PathProximityTextureProviderConfiguration => new
            PathProximityTextureProviderConfiguration()
            {
                MaxProximity = 5
            };

        public SpatialDbConfiguration PathProximityTextureDatabaseConfiguration => new SpatialDbConfiguration()
        {
            QueryingCellSize = new Vector2(90, 90)
        };

        public bool EngraveRoadsInTerrain = true;


        public RoadEngravingTerrainFeatureApplierConfiguration RoadEngravingTerrainFetureApplierConfiguration =
            new RoadEngravingTerrainFeatureApplierConfiguration()
            {
            };

        public RoadEngraver.RoadEngraverConfiguration RoadEngraverConfiguration =
            new RoadEngraver.RoadEngraverConfiguration()
            {

            MaxProximity = RoadDefaultConstants.MaxProximity,
            MaxDelta = RoadDefaultConstants.MaxDelta,
            StartSlopeProximity = RoadDefaultConstants.StartSlopeProximity,
            EndSlopeProximity = RoadDefaultConstants.EndSlopeProximity
            };



        public string StainTerrainServicePath => _filePathsConfiguration.StainTerrainServicePath;

        ///////////////////// Terrain Shape Db
        public InMemoryCacheConfiguration InMemoryCacheConfiguration =
            new InMemoryCacheConfiguration()
            {
                MaxTextureMemoryUsed = 1024 * 1024 * 512 * 2
            };

        // details
        public TerrainDetailGeneratorConfiguration TerrainDetailGeneratorConfiguration =
            new TerrainDetailGeneratorConfiguration()
            {
                TerrainDetailImageSideDisjointResolution = 240
            };

        public TerrainShapeDbConfiguration  TerrainShapeDbConfiguration = new TerrainShapeDbConfiguration()
        {
            UseTextureLoadingFromDisk = true,
            UseTextureSavingToDisk = true,
            MergeTerrainDetail = false
        };

        public string TerrainDetailCachePath => _filePathsConfiguration.TerrainDetailCachePath;


        public MyUtSchedulerConfiguration SchedulerConfiguration = new MyUtSchedulerConfiguration()
        {
            FreeTimeAmountAllowingForUpdate = 1 / 50f,
            TargetFrameTime = 1 / 30f,
            MaxFreeTime = 1 / 30f,
            FreeTimeSubtractingMultiplier = 1,
            SchedulingEnabled = true, 
        };

        public UltraUpdatableContainerConfiguration UpdatableContainerConfiguration => new UltraUpdatableContainerConfiguration()
        {
            ServicesProfilingEnabled = true
        };
    }

}