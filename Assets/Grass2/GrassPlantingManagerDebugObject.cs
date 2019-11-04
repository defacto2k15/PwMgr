using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Grass;
using Assets.Grass2.Groups;
using Assets.Grass2.IntensitySampling;
using Assets.Grass2.Planting;
using Assets.Grass2.PositionResolving;
using Assets.Grass2.Types;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Grass2
{
    public class GrassPlantingManagerDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;
        private DebugGrassPlanterUnderTest _debugGrassPlanterUnderTest;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            _debugGrassPlanterUnderTest = new DebugGrassPlanterUnderTest();
            _debugGrassPlanterUnderTest.Start(ComputeShaderContainer);
            var grassGroupsPlanter = _debugGrassPlanterUnderTest.GrassGroupsPlanter;

            var generationArea = new MyRectangle(0, 0, 30, 30);

            var randomFigureGenerator = new RandomFieldFigureGeneratorUTProxy(
                new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                    new Ring2RandomFieldFigureGeneratorConfiguration()
                    {
                        PixelsPerUnit = new Vector2(1, 1)
                    }));
            var randomFigure = randomFigureGenerator.GenerateRandomFieldFigureAsync(
                RandomFieldNature.FractalSimpleValueNoise3, 312,
                new MyRectangle(0, 0, 30, 30)).Result;

            var intensityFigureProvider = new IntensityFromRandomFiguresCompositionProvider(
                PoissonDiskSamplingDebugObject.CreateDebugRandomFieldFigure(),
                randomFigure, 0.3f);

            grassGroupsPlanter.AddGrassGroup(generationArea, GrassType.Debug1, intensityFigureProvider);

            _debugGrassPlanterUnderTest.FinalizeStart();
        }

        public void Update()
        {
            _debugGrassPlanterUnderTest.Update();
        }

        public static UpdatedTerrainTextures GenerateDebugUpdatedTerrainTextures()
        {
            var height = 1;
            var heightArray = new float[12, 12];
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    heightArray[x, y] = height;
                }
            }
            var ha = new HeightmapArray(heightArray);
            var encodedHeightTex = HeightmapUtils.CreateTextureFromHeightmap(ha);

            var transformer = new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());
            var plainHeightTex =
                transformer.EncodedHeightTextureToPlain(TextureWithSize.FromTex2D(encodedHeightTex));

            var normalArray = new Vector3[12, 12];
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    normalArray[x, y] = new Vector3(0.0f, 0.0f, 1.0f).normalized;
                }
            }
            var normalTexture = HeightmapUtils.CreateNormalTexture(normalArray);

            return (new UpdatedTerrainTextures()
            {
                HeightTexture = plainHeightTex,
                NormalTexture = normalTexture,
                TextureCoords = new MyRectangle(0, 0, 1, 1),
                TextureGlobalPosition = new MyRectangle(0, 0, 90, 90),
            });
        }
    }

    public interface IDebugPlanterUnderTest
    {
        void Start(ComputeShaderContainerGameObject computeShaderContainer);
        GrassGroupsPlanter GrassGroupsPlanter { get; }
        void FinalizeStart();
        void Update();
    }

    public class DebugGrassPlanterUnderTest : IDebugPlanterUnderTest
    {
        private GrassGroupsPlanter _grassGroupsPlanter;
        private GlobalGpuInstancingContainer _globalGpuInstancingContainer;
        private DesignBodySpotUpdaterProxy _designBodySpotUpdaterProxy;

        public void Start(ComputeShaderContainerGameObject computeShaderContainer)
        {
            var commonExecutor = new CommonExecutorUTProxy();
            var shaderExecutorObject = new UnityThreadComputeShaderExecutorObject();

            var updater =
                new DesignBodySpotUpdater(new DesignBodySpotChangeCalculator(computeShaderContainer,
                    shaderExecutorObject, commonExecutor, HeightDenormalizer.Identity));

            _designBodySpotUpdaterProxy = new DesignBodySpotUpdaterProxy(updater);
            updater.SetChangesListener(new LambdaSpotPositionChangesListener(null, dict =>
            {
                foreach (var pair in dict)
                {
                    _grassGroupsPlanter.GrassGroupSpotChanged(pair.Key, pair.Value);
                }
            }));
            _designBodySpotUpdaterProxy.StartThreading(() => { });


            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBladeMesh(1);

            var instancingMaterial = new Material(Shader.Find("Custom/Vegetation/Grass.Instanced"));
            instancingMaterial.enableInstancing = true;

            var commonUniforms = new UniformsPack();
            commonUniforms.SetUniform("_BendingStrength", 0.0f);
            commonUniforms.SetUniform("_WindDirection", Vector4.one);

            var instancingContainer = new GpuInstancingVegetationSubjectContainer(
                new GpuInstancerCommonData(mesh, instancingMaterial, commonUniforms),
                new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                {
                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_InitialBendingValue", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantBendingStiffness", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantDirection", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_RandSeed", GpuInstancingUniformType.Float),
                })
            );

            _globalGpuInstancingContainer = new GlobalGpuInstancingContainer();
            var bucketId = _globalGpuInstancingContainer.CreateBucket(instancingContainer);
            GrassGroupsContainer grassGroupsContainer =
                new GrassGroupsContainer(_globalGpuInstancingContainer, bucketId);

            IGrassPositionResolver grassPositionResolver = new SimpleRandomSamplerPositionResolver();

            GrassDetailInstancer grassDetailInstancer = new GrassDetailInstancer();

            _grassGroupsPlanter = new GrassGroupsPlanter(
                grassDetailInstancer, grassPositionResolver, grassGroupsContainer, _designBodySpotUpdaterProxy,
                new LegacyGrass2BladeAspectsGenerator(),
                GrassDebugUtils.TemplatesDictionary, Repositioner.Identity);
        }

        public void FinalizeStart()
        {
            Debug.Log("I9 Start finalized");
            //_designBodySpotUpdaterProxy.SynchronicUpdate();
            _designBodySpotUpdaterProxy.UpdateBodiesSpots(GrassPlantingManagerDebugObject
                .GenerateDebugUpdatedTerrainTextures());
            _globalGpuInstancingContainer.StartThread();
            _designBodySpotUpdaterProxy.SynchronicUpdate();
            _globalGpuInstancingContainer.FinishUpdateBatch();
        }

        public void Update()
        {
            _globalGpuInstancingContainer.DrawFrame();
        }

        public GrassGroupsPlanter GrassGroupsPlanter => _grassGroupsPlanter;
    }

    public static class GrassDebugUtils
    {
        public static Dictionary<GrassType, GrassTypeTemplate> TemplatesDictionary => new
            Dictionary<GrassType, GrassTypeTemplate>()
            {
                {
                    GrassType.Debug1, new GrassTypeTemplate()
                    {
                        Color = new Triplet<RandomCharacteristics>()
                        {
                            A = new RandomCharacteristics()
                            {
                                Mean = 0.8f,
                                StandardDeviation = 0.05f,
                            },
                            B = new RandomCharacteristics()
                            {
                                Mean = 0.0f,
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
                                StandardDeviation = 0.01f,
                                Mean = 0.1f,
                            },
                            B = new RandomCharacteristics() //height
                            {
                                Mean = 0.7f,
                                StandardDeviation = 0.1f
                            }
                        },
                        InstancesPerUnitSquare = 5
                    }
                },
                {
                    GrassType.Debug2, new GrassTypeTemplate()
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
                                StandardDeviation = 0.03f,
                                Mean = 0.1f,
                            },
                            B = new RandomCharacteristics() //height
                            {
                                Mean = 1.7f,
                                StandardDeviation = 0.1f
                            }
                        },
                        InstancesPerUnitSquare = 3
                    }
                },
            };

        public static Dictionary<GrassType, GrassTypeTemplate> BushTemplates => new
            Dictionary<GrassType, GrassTypeTemplate>()
            {
                {
                    GrassType.Debug1, new GrassTypeTemplate()
                    {
                        Color = new Triplet<RandomCharacteristics>()
                        {
                            A = new RandomCharacteristics()
                            {
                                Mean = 0.8f,
                                StandardDeviation = 0.05f,
                            },
                            B = new RandomCharacteristics()
                            {
                                Mean = 0.0f,
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
                            StandardDeviation = 0.0f,
                        },
                        FlatSize = new Pair<RandomCharacteristics>()
                        {
                            A = new RandomCharacteristics() //width
                            {
                                StandardDeviation = 0.4f,
                                Mean = 3.1f,
                            },
                            B = new RandomCharacteristics() //height
                            {
                                Mean = 3.1f,
                                StandardDeviation = 0.4f
                            }
                        },
                        InstancesPerUnitSquare = 5
                    }
                },
                {
                    GrassType.Debug2, new GrassTypeTemplate()
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
                                Mean = 3.1f,
                            },
                            B = new RandomCharacteristics() //height
                            {
                                Mean = 3.1f,
                                StandardDeviation = 0.4f
                            }
                        },
                        InstancesPerUnitSquare = 3
                    }
                },
            };
    }
}