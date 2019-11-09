using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.Erosion;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Random;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainDetailProviderDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;
        public Texture MainTexture;
        public Texture2D OutTexture;

        private UTTextureRendererProxy _utTextureRendererProxy;
        private GameObject _gameObject;

        private TerrainTextureFormatTransformator _transformator =
            new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());


        public void StartX()
        {
            TaskUtils.SetGlobalMultithreading(false);
            MainTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\n49_e019_1arc_v3.png", 3600, 3600,
                TextureFormat.ARGB32, true, false);

            var plain = _transformator.EncodedHeightTextureToPlainAsync(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = MainTexture
            });

            var decoded = _transformator.PlainToEncodedHeightTextureAsync(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = plain.Result
            });

            SavingFileManager.SaveTextureToPngFile(@"C:\inz\cont\n49_e019_1arc_v4.rfloat.png", decoded.Result);
        }

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var rgbaMainTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\n49_e019_1arc_v3.png", 3600,
                3600,
                TextureFormat.ARGB32, true, false);
            MainTexture = _transformator.EncodedHeightTextureToPlainAsync(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = rgbaMainTexture
            }).Result;

            //var heightArray = HeightmapUtils.CreateHeightmapArrayFromTexture(MainTexture);
            //for (int x = 0; x < 5; x++)
            //{
            //    for (int y = 0; y < 5; y++)
            //    {
            //        heightArray.HeightmapAsArray[x, y] = 0.3f;
            //    }
            //}
            //MainTexture = HeightmapUtils.CreateTextureFromHeightmap(heightArray);


            InitializeFields();

            var unitySideLength = 10f;
            var realSideLength = 240 * 24;
            var metersPerUnit = realSideLength / unitySideLength;

            _gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Terrain_Debug_Plain"));
            _gameObject.GetComponent<MeshRenderer>().material = material;
            _gameObject.name = "Terrain";
            _gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);

            //_gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit) , 10);
            //_gameObject.transform.localPosition = new Vector3(0 , 0, 0);

            //_gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit) * 8, 10);
            //_gameObject.transform.localPosition = new Vector3(0, 0, 0);

            _gameObject.transform.localScale = new Vector3(10, 1 / 64f * (6500 / metersPerUnit) * 8 * 8, 10);
            _gameObject.transform.localPosition = new Vector3(0, -30, 0);
            _gameObject.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);

            RegenerateTextures();

            //Camera.main.transform.localPosition = new Vector3(10,2,0);
            //Camera.main.transform.eulerAngles = new Vector3(30,-45,0);
            Camera.main.transform.localPosition = new Vector3(-1, -25, 10);
            Camera.main.transform.eulerAngles = new Vector3(30, 132, 0);
        }

        private void RegenerateTextures()
        {
            var material = _gameObject.GetComponent<MeshRenderer>().material;
            var tex0 = CreateGenerateAndGenerateTexture(new List<RankedTerrainFeatureApplier>()
            {
                //new RankedTerrainFeatureApplier()
                //{
                //    Rank = 1,
                //    Applier = new RandomNoiseTerrainFeatureApplier(_utTextureRendererProxy, new CommonExecutorUTProxy(),
                //        new Dictionary<TerrainCardinalResolution, RandomNoiseTerrainFeatureApplierConfiguration>
                //        {
                //            //{
                //            //    TerrainCardinalResolution.MIN_RESOLUTION,
                //            //    new RandomNoiseTerrainFeatureApplierConfiguration()
                //            //    {
                //            //        DetailResolutionMultiplier = 1,
                //            //        NoiseStrengthMultiplier = 1
                //            //    }
                //            //},
                //            //{
                //            //    TerrainCardinalResolution.MID_RESOLUTION,
                //            //    new RandomNoiseTerrainFeatureApplierConfiguration()
                //            //    {
                //            //        DetailResolutionMultiplier = 8,
                //            //        NoiseStrengthMultiplier = 0.45f
                //            //    }
                //            //},
                //            {
                //                TerrainCardinalResolution.MAX_RESOLUTION,
                //                new RandomNoiseTerrainFeatureApplierConfiguration()
                //                {
                //                    DetailResolutionMultiplier = 8*8,
                //                    NoiseStrengthMultiplier = 0.45f/9f
                //                }
                //            },
                //        }),
                //    AvalibleResolutions = new List<TerrainCardinalResolution>()
                //    {
                //        //TerrainCardinalResolution.MIN_RESOLUTION,
                //        //TerrainCardinalResolution.MID_RESOLUTION,
                //        //TerrainCardinalResolution.MAX_RESOLUTION,
                //    }
                //},
                //new RankedTerrainFeatureApplier()
                //{
                //    Rank = 2,
                //    Applier = new DiamondSquareTerrainFeatureApplier(
                //        new RandomProviderGenerator(123), new CommonExecutorUTProxy(),
                //        _utTextureRendererProxy,
                //        new Dictionary<TerrainCardinalResolution, DiamondSquareTerrainFeatureApplierConfiguration>
                //        {
                //            //{
                //            //    TerrainCardinalResolution.MIN_RESOLUTION,
                //            //    new DiamondSquareTerrainFeatureApplierConfiguration
                //            //    {
                //            //        DiamondSquareWorkingArrayLength = 32,
                //            //        DiamondSquareWeight = 0.012f
                //            //    }
                //            //},
                //            //{
                //            //    TerrainCardinalResolution.MID_RESOLUTION,
                //            //    new DiamondSquareTerrainFeatureApplierConfiguration
                //            //    {
                //            //        DiamondSquareWorkingArrayLength = 32,
                //            //        DiamondSquareWeight = 0.0025f
                //            //    }
                //            //},
                //            {
                //                TerrainCardinalResolution.MAX_RESOLUTION,
                //                new DiamondSquareTerrainFeatureApplierConfiguration
                //                {
                //                    DiamondSquareWorkingArrayLength = 32,
                //                    DiamondSquareWeight = 0.0003f
                //                }
                //            },
                //        }),
                //    AvalibleResolutions = new List<TerrainCardinalResolution>()
                //    {
                //        //TerrainCardinalResolution.MIN_RESOLUTION,
                //        //TerrainCardinalResolution.MID_RESOLUTION,
                //        //TerrainCardinalResolution.MAX_RESOLUTION,
                //    }
                //},
                //new RankedTerrainFeatureApplier()
                //{
                //    Rank = 3,
                //    Applier = new ThermalErosionTerrainFeatureApplier(ContainerGameObject, new UnityThreadComputeShaderExecutorObject(), new CommonExecutorUTProxy(),
                //    new Dictionary<TerrainCardinalResolution, ThermalErosionTerrainFeatureApplierConfiguration>
                //    {
                //        { TerrainCardinalResolution.MIN_RESOLUTION, new ThermalErosionTerrainFeatureApplierConfiguration
                //        {

                //        } },
                //        //{ TerrainCardinalResolution.MID_RESOLUTION, new ThermalErosionTerrainFeatureApplierConfiguration
                //        //{
                //        //    TParam = 0.001f/2.3f,
                //        //    CParam = 0.06f
                //        //} },
                //    }),
                //    AvalibleResolutions = new List<TerrainCardinalResolution>()
                //    {
                //        //TerrainCardinalResolution.MIN_RESOLUTION,
                //        //TerrainCardinalResolution.MID_RESOLUTION
                //    }
                //},
                //new RankedTerrainFeatureApplier()
                //{
                //    Rank = 4,
                //    Applier = new HydraulicErosionTerrainFeatureApplier(ContainerGameObject,
                //        new UnityThreadComputeShaderExecutorObject(),
                //        new Dictionary<TerrainCardinalResolution, HydraulicEroderConfiguration>()
                //        {
                //            //{
                //            //    TerrainCardinalResolution.MIN_RESOLUTION, new HydraulicEroderConfiguration()
                //            //    {
                //            //        StepCount = 20,
                //            //        kr_ConstantWaterAddition = 0.000002f, // 0.0001f,
                //            //        ks_GroundToSedimentFactor = 1f,
                //            //        ke_WaterEvaporationFactor = 0.05f,
                //            //        kc_MaxSedimentationFactor = 0.8f,
                //            //    }
                //            //},
                //            //{
                //            //    TerrainCardinalResolution.MID_RESOLUTION, new HydraulicEroderConfiguration()
                //            //    {
                //            //        StepCount = 20,
                //            //        kr_ConstantWaterAddition = 0.000002f, // 0.0001f,
                //            //        ks_GroundToSedimentFactor = 1f,
                //            //        ke_WaterEvaporationFactor = 0.05f,
                //            //        kc_MaxSedimentationFactor = 0.8f / 2f,
                //            //    }
                //            //},
                //            {
                //                TerrainCardinalResolution.MAX_RESOLUTION, new HydraulicEroderConfiguration()
                //                {
                //                    StepCount = 20,
                //                    kr_ConstantWaterAddition = 0.0000035f, // 0.0001f,
                //                    ks_GroundToSedimentFactor = 1f,
                //                    ke_WaterEvaporationFactor = 0.05f,
                //                    kc_MaxSedimentationFactor = 0.8f / 4f,
                //                }
                //            }
                //        }),
                //    AvalibleResolutions = new List<TerrainCardinalResolution>()
                //    {
                //        //TerrainCardinalResolution.MIN_RESOLUTION,
                //        //TerrainCardinalResolution.MID_RESOLUTION,
                //        TerrainCardinalResolution.MAX_RESOLUTION,
                //    }
                //},
                //new RankedTerrainFeatureApplier()
                //{
                //    Rank = 5,
                //    Applier = new TweakedThermalErosionTerrainFeatureApplier(ContainerGameObject,
                //        new UnityThreadComputeShaderExecutorObject(),
                //        new Dictionary<TerrainCardinalResolution,
                //            TweakedThermalErosionTerrainFeatureApplierConfiguration>
                //        {
                //            //{
                //            //    TerrainCardinalResolution.MIN_RESOLUTION,
                //            //    new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                //            //    {

                //            //    }
                //            //},
                //            //{
                //            //    TerrainCardinalResolution.MID_RESOLUTION,
                //            //    new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                //            //    {
                //            //        TParam = 0.001f,
                //            //        CParam = 0.008f

                //            //    }
                //            //},
                //            {
                //                TerrainCardinalResolution.MAX_RESOLUTION,
                //                new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                //                {
                //                    TParam = 0.001f,
                //                    CParam = 0.008f

                //                }
                //            },
                //        }),
                //    AvalibleResolutions = new List<TerrainCardinalResolution>()
                //    {
                //        //TerrainCardinalResolution.MIN_RESOLUTION,
                //        //TerrainCardinalResolution.MID_RESOLUTION,
                //        //TerrainCardinalResolution.MAX_RESOLUTION,
                //    }
                //},
            });

            //////////////////////////////////////////////////

            var tex1 = CreateGenerateAndGenerateTexture(new List<RankedTerrainFeatureApplier>()
            {
                new RankedTerrainFeatureApplier()
                {
                    Rank = 1,
                    Applier = new RandomNoiseTerrainFeatureApplier(_utTextureRendererProxy, new CommonExecutorUTProxy(),
                        new Dictionary<TerrainCardinalResolution, RandomNoiseTerrainFeatureApplierConfiguration>
                        {
                            //{
                            //    TerrainCardinalResolution.MIN_RESOLUTION,
                            //    new RandomNoiseTerrainFeatureApplierConfiguration()
                            //    {
                            //        DetailResolutionMultiplier = 1,
                            //        NoiseStrengthMultiplier = 1
                            //    }
                            //},
                            //{
                            //    TerrainCardinalResolution.MID_RESOLUTION,
                            //    new RandomNoiseTerrainFeatureApplierConfiguration()
                            //    {
                            //        DetailResolutionMultiplier = 8,
                            //        NoiseStrengthMultiplier = 0.45f
                            //    }
                            //},
                            //{
                            //    TerrainCardinalResolution.MAX_RESOLUTION,
                            //    new RandomNoiseTerrainFeatureApplierConfiguration()
                            //    {
                            //        DetailResolutionMultiplier = 8*8,
                            //        NoiseStrengthMultiplier = 0.45f/9f
                            //    }
                            //},
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        //TerrainCardinalResolution.MIN_RESOLUTION,
                        //TerrainCardinalResolution.MID_RESOLUTION,
                        //TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 2,
                    Applier = new DiamondSquareTerrainFeatureApplier(
                        new RandomProviderGenerator(123), new CommonExecutorUTProxy(),
                        _utTextureRendererProxy,
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
                            //{
                            //    TerrainCardinalResolution.MID_RESOLUTION,
                            //    new DiamondSquareTerrainFeatureApplierConfiguration
                            //    {
                            //        DiamondSquareWorkingArrayLength = 32,
                            //        DiamondSquareWeight = 0.0025f
                            //    }
                            //},
                            //{
                            //    TerrainCardinalResolution.MAX_RESOLUTION,
                            //    new DiamondSquareTerrainFeatureApplierConfiguration
                            //    {
                            //        DiamondSquareWorkingArrayLength = 32,
                            //        DiamondSquareWeight = 0.0003f
                            //    }
                            //},
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        TerrainCardinalResolution.MIN_RESOLUTION,
                        //TerrainCardinalResolution.MID_RESOLUTION,
                        //TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 3,
                    Applier = new ThermalErosionTerrainFeatureApplier(ContainerGameObject,
                        new UnityThreadComputeShaderExecutorObject(), new CommonExecutorUTProxy(),
                        new Dictionary<TerrainCardinalResolution, ThermalErosionTerrainFeatureApplierConfiguration>
                        {
                            {
                                TerrainCardinalResolution.MIN_RESOLUTION,
                                new ThermalErosionTerrainFeatureApplierConfiguration
                                {
                                }
                            },
                            //{ TerrainCardinalResolution.MID_RESOLUTION, new ThermalErosionTerrainFeatureApplierConfiguration
                            //{
                            //    TParam = 0.001f/2.3f,
                            //    CParam = 0.06f
                            //} },
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        //TerrainCardinalResolution.MIN_RESOLUTION,
                        //TerrainCardinalResolution.MID_RESOLUTION
                        //TerrainCardinalResolution.MAX_RESOLUTION
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 4,
                    Applier = new HydraulicErosionTerrainFeatureApplier(ContainerGameObject,
                        new UnityThreadComputeShaderExecutorObject(),
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
                            //{
                            //    TerrainCardinalResolution.MID_RESOLUTION, new HydraulicEroderConfiguration()
                            //    {
                            //        StepCount = 20,
                            //        kr_ConstantWaterAddition = 0.000002f, // 0.0001f,
                            //        ks_GroundToSedimentFactor = 1f,
                            //        ke_WaterEvaporationFactor = 0.05f,
                            //        kc_MaxSedimentationFactor = 0.8f / 2f,
                            //    }
                            //},
                            //{
                            //    TerrainCardinalResolution.MAX_RESOLUTION, new HydraulicEroderConfiguration()
                            //    {
                            //        StepCount = 20,
                            //        kr_ConstantWaterAddition = 0.0000035f, // 0.0001f,
                            //        ks_GroundToSedimentFactor = 1f,
                            //        ke_WaterEvaporationFactor = 0.05f,
                            //        kc_MaxSedimentationFactor = 0.8f / 4f,
                            //    }
                            //}
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        //TerrainCardinalResolution.MIN_RESOLUTION,
                        //TerrainCardinalResolution.MID_RESOLUTION,
                        //TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
                new RankedTerrainFeatureApplier()
                {
                    Rank = 5,
                    Applier = new TweakedThermalErosionTerrainFeatureApplier(ContainerGameObject,
                        new UnityThreadComputeShaderExecutorObject(),
                        new Dictionary<TerrainCardinalResolution,
                            TweakedThermalErosionTerrainFeatureApplierConfiguration>
                        {
                            {
                                TerrainCardinalResolution.MIN_RESOLUTION,
                                new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                                {
                                }
                            },
                            //{
                            //    TerrainCardinalResolution.MID_RESOLUTION,
                            //    new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                            //    {
                            //        TParam = 0.001f,
                            //        CParam = 0.008f

                            //    }
                            //},
                            //{
                            //    TerrainCardinalResolution.MAX_RESOLUTION,
                            //    new TweakedThermalErosionTerrainFeatureApplierConfiguration()
                            //    {
                            //        TParam = 0.001f,
                            //        CParam = 0.008f

                            //    }
                            //},
                        }),
                    AvalibleResolutions = new List<TerrainCardinalResolution>()
                    {
                        //TerrainCardinalResolution.MIN_RESOLUTION,
                        //TerrainCardinalResolution.MID_RESOLUTION,
                        //TerrainCardinalResolution.MAX_RESOLUTION,
                    }
                },
            });


            material.SetTexture("_HeightmapTex0", tex0);
            material.SetTexture("_HeightmapTex1", tex1);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                RegenerateTextures();
            }
        }

        private void InitializeFields()
        {
            TextureRendererServiceConfiguration rendererServiceConfiguration = new TextureRendererServiceConfiguration()
            {
                StepSize = new Vector2(500, 500)
            };
            _utTextureRendererProxy = new UTTextureRendererProxy(
                new TextureRendererService(new MultistepTextureRenderer(ContainerGameObject),
                    rendererServiceConfiguration));
        }

        private TerrainDetailProvider CreateTerrainDetailProvider(List<RankedTerrainFeatureApplier> featureAppliers)
        {
            TerrainDetailGeneratorConfiguration generatorConfiguration = new TerrainDetailGeneratorConfiguration()
            {
                TerrainDetailImageSideDisjointResolution = 240
            };
            TextureWithCoords fullFundationData = new TextureWithCoords(new TextureWithSize()
            {
                Texture = MainTexture,
                Size = new IntVector2(MainTexture.width, MainTexture.height)
            }, new MyRectangle(0, 0, 3601 * 24, 3601 * 24));

            TerrainDetailGenerator generator =
                new TerrainDetailGenerator(generatorConfiguration, _utTextureRendererProxy, fullFundationData,
                    featureAppliers, new CommonExecutorUTProxy());

            var provider = new TerrainDetailProvider( generator, null, new TerrainDetailAlignmentCalculator(240));
            generator.SetBaseTerrainDetailProvider(BaseTerrainDetailProvider.CreateFrom(provider));
            return provider;
        }

        private Texture CreateGenerateAndGenerateTexture(List<RankedTerrainFeatureApplier> featureAppliers)
        {
            var provider = CreateTerrainDetailProvider(featureAppliers);

            //UnityCoordsPositions2D queryArea = new UnityCoordsPositions2D(0, 0, 24 * 240, 24 * 240);
            //var resolution = TerrainCardinalResolution.MIN_RESOLUTION;

            //UnityCoordsPositions2D queryArea = new UnityCoordsPositions2D(3 * 240, 3 * 240, 3 * 240, 3 * 240);
            //var resolution = TerrainCardinalResolution.MID_RESOLUTION;

            MyRectangle queryArea = new MyRectangle(5 * 64 * 0.375f * 240, 64 * 5 * 0.375f * 240,
                64 * 0.375f * 240, 64 * 0.375f * 240);
            var resolution = TerrainCardinalResolution.MIN_RESOLUTION;

            var outTex = provider.GenerateHeightDetailElementAsync(queryArea, resolution, CornersMergeStatus.NOT_MERGED).Result.Texture;
            return outTex.Texture;
        }

        private void CreateDebObject(string goName, int i, Texture outTex)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Standard"));
            material.SetTexture("_MainTex", outTex);
            go.GetComponent<MeshRenderer>().material = material;

            go.name = goName;

            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new Vector3(10, 10, 1);
            go.transform.localPosition = new Vector3(10 * i, 0, 0);
        }
    }
}