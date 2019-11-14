using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.Erosion;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Random;
using Assets.Roads.TerrainFeature;
using Assets.Utils.Services;

namespace Assets.FinalExecution
{
    public class FinalTerrainFeatureAppliers
    {

        public static List<RankedTerrainFeatureApplier> CreateFeatureAppliers(
            UTTextureRendererProxy utTextureRendererProxy,
            ComputeShaderContainerGameObject computeShaderContainerGameObject,
            CommonExecutorUTProxy commonExecutor,
            UnityThreadComputeShaderExecutorObject threadComputeShaderExecutorObject)
        {
            var toReturn = new List<RankedTerrainFeatureApplier>()
            {
                new RankedTerrainFeatureApplier()
                {
                    Rank = 1,
                    Applier = new RandomNoiseTerrainFeatureApplier(utTextureRendererProxy, commonExecutor,
                        new Dictionary<TerrainCardinalResolution, RandomNoiseTerrainFeatureApplierConfiguration>
                        {
                            {
                                TerrainCardinalResolution.MIN_RESOLUTION,
                                new RandomNoiseTerrainFeatureApplierConfiguration()
                                {
                                    DetailResolutionMultiplier = 1,
                                    NoiseStrengthMultiplier = 1
                                }
                            },
                            {
                                TerrainCardinalResolution.MID_RESOLUTION,
                                new RandomNoiseTerrainFeatureApplierConfiguration()
                                {
                                    DetailResolutionMultiplier = 8,
                                    NoiseStrengthMultiplier = 0.45f
                                }
                            },
                            {
                                TerrainCardinalResolution.MAX_RESOLUTION,
                                new RandomNoiseTerrainFeatureApplierConfiguration()
                                {
                                    DetailResolutionMultiplier = 8 * 8,
                                    NoiseStrengthMultiplier = 4.7f * 0.45f / 9f //todo CHANGE IT !!
                                }
                            },
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        TerrainCardinalResolution.MIN_RESOLUTION,
                        TerrainCardinalResolution.MID_RESOLUTION,
                        TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 2,
                    Applier = new DiamondSquareTerrainFeatureApplier(
                        new RandomProviderGenerator(123), commonExecutor,
                        utTextureRendererProxy,
                        new Dictionary<TerrainCardinalResolution, DiamondSquareTerrainFeatureApplierConfiguration>
                        {
                            {
                                TerrainCardinalResolution.MIN_RESOLUTION,
                                new DiamondSquareTerrainFeatureApplierConfiguration
                                {
                                    DiamondSquareWorkingArrayLength = 32,
                                    DiamondSquareWeight = 0.012f
                                }
                            },
                            {
                                TerrainCardinalResolution.MID_RESOLUTION,
                                new DiamondSquareTerrainFeatureApplierConfiguration
                                {
                                    DiamondSquareWorkingArrayLength = 32,
                                    DiamondSquareWeight = 0.0025f * 1.3f
                                }
                            },
                            {
                                TerrainCardinalResolution.MAX_RESOLUTION,
                                new DiamondSquareTerrainFeatureApplierConfiguration
                                {
                                    DiamondSquareWorkingArrayLength = 32,
                                    DiamondSquareWeight = 0.0003f
                                }
                            },
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        TerrainCardinalResolution.MIN_RESOLUTION,
                        TerrainCardinalResolution.MID_RESOLUTION,
                        TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 3,
                    Applier = new ThermalErosionTerrainFeatureApplier(computeShaderContainerGameObject,
                        threadComputeShaderExecutorObject, commonExecutor,
                        new Dictionary<TerrainCardinalResolution, ThermalErosionTerrainFeatureApplierConfiguration>
                        {
                            {
                                TerrainCardinalResolution.MIN_RESOLUTION,
                                new ThermalErosionTerrainFeatureApplierConfiguration
                                {
                                }
                            },
                            {
                                TerrainCardinalResolution.MID_RESOLUTION,
                                new ThermalErosionTerrainFeatureApplierConfiguration
                                {
                                    TParam = 0.001f / 2.3f,
                                    CParam = 0.06f
                                }
                            },
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        TerrainCardinalResolution.MIN_RESOLUTION,
                        TerrainCardinalResolution.MID_RESOLUTION
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 4,
                    Applier = new HydraulicErosionTerrainFeatureApplier(computeShaderContainerGameObject,
                        threadComputeShaderExecutorObject,
                        new Dictionary<TerrainCardinalResolution, HydraulicEroderConfiguration>()
                        {
                            {
                                TerrainCardinalResolution.MIN_RESOLUTION, new HydraulicEroderConfiguration()
                                {
                                    StepCount = 20,
                                    kr_ConstantWaterAddition = 0.000002f, // 0.0001f,
                                    ks_GroundToSedimentFactor = 1f,
                                    ke_WaterEvaporationFactor = 0.05f,
                                    kc_MaxSedimentationFactor = 0.8f,
                                }
                            },
                            {
                                TerrainCardinalResolution.MID_RESOLUTION, new HydraulicEroderConfiguration()
                                {
                                    StepCount = 20,
                                    kr_ConstantWaterAddition = 0.000002f, // 0.0001f,
                                    ks_GroundToSedimentFactor = 1f,
                                    ke_WaterEvaporationFactor = 0.05f,
                                    kc_MaxSedimentationFactor = 0.8f / 2f,
                                }
                            },
                            {
                                TerrainCardinalResolution.MAX_RESOLUTION, new HydraulicEroderConfiguration()
                                {
                                    StepCount = 20,
                                    kr_ConstantWaterAddition = 0.0000035f, // 0.0001f,
                                    ks_GroundToSedimentFactor = 1f,
                                    ke_WaterEvaporationFactor = 0.05f,
                                    kc_MaxSedimentationFactor = 0.8f / 4f,
                                }
                            }
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        TerrainCardinalResolution.MIN_RESOLUTION,
                        TerrainCardinalResolution.MID_RESOLUTION,
                        TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 5,
                    Applier = new TweakedThermalErosionTerrainFeatureApplier(computeShaderContainerGameObject,
                        threadComputeShaderExecutorObject,
                        new Dictionary<TerrainCardinalResolution,
                            TweakedThermalErosionTerrainFeatureApplierConfiguration>
                        {
                            {
                                TerrainCardinalResolution.MIN_RESOLUTION,
                                new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                                {
                                }
                            },
                            {
                                TerrainCardinalResolution.MID_RESOLUTION,
                                new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                                {
                                    TParam = 0.001f,
                                    CParam = 0.008f
                                }
                            },
                            {
                                TerrainCardinalResolution.MAX_RESOLUTION,
                                new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                                {
                                    TParam = 0.001f,
                                    CParam = 0.008f
                                }
                            },
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        TerrainCardinalResolution.MIN_RESOLUTION,
                        TerrainCardinalResolution.MID_RESOLUTION,
                        TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
            };

            return toReturn;
        }


        public static RankedTerrainFeatureApplier CreateRoadEngravingApplier(RoadEngravingTerrainFeatureApplier roadApplier)
        {
            return new RankedTerrainFeatureApplier()
            {
                Applier = roadApplier,
                AvalibleResolutions = new List<TerrainCardinalResolution>()
                {
                    TerrainCardinalResolution.MAX_RESOLUTION
                },
                Rank = 10
            };
        }
    }
}