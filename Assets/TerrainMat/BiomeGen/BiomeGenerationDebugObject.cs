using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Random;
using Assets.TerrainMat.Stain;
using Assets.Trees.DesignBodyDetails;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.TerrainMat.BiomeGen
{
    public class BiomeGenerationDebugObject : MonoBehaviour
    {
        public GameObject RenderTextureGameObject;

        private void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var gen2 = new BiomeInstancesContainerGenerator(new BiomesContainerGeneratorConfiguration()
            {
                GenerationSpaceSize = new Vector2(0.25f, 0.25f)
            });


            var container = gen2.Generate(
                new BiomesContainerConfiguration()
                {
                    HighQualityQueryDistance = 0.003f,
                    Center = new Vector2(0.5f, 0.5f),
                    DefaultType = BiomeType.Forest
                },
                new List<BiomeInstancePlacementTemplate>()
                {
                    new BiomeInstancePlacementTemplate()
                    {
                        LeafPointsDistanceCharacteristics = new RandomCharacteristics()
                        {
                            Mean = 0.01f,
                            StandardDeviation = 0.01f
                        },
                        LeafPointsCount = new RandomCharacteristics()
                        {
                            Mean = 16,
                            StandardDeviation = 2
                        },
                        OccurencesPerSquareUnit = new RandomCharacteristics()
                        {
                            //StandardDeviation = 5,
                            //Mean = 3
                            Mean = 1 / (0.25f * 0.25f),
                            StandardDeviation = 0,
                        },
                        Type = BiomeType.Grass,
                        LeafPointsGenerator = new LeafPointsGenerator()
                    },
                    new BiomeInstancePlacementTemplate()
                    {
                        LeafPointsDistanceCharacteristics = new RandomCharacteristics()
                        {
                            Mean = 0.01f,
                            StandardDeviation = 0.01f
                        },
                        LeafPointsCount = new RandomCharacteristics()
                        {
                            Mean = 16,
                            StandardDeviation = 2
                        },
                        OccurencesPerSquareUnit = new RandomCharacteristics()
                        {
                            Mean = 1 / (0.25f * 0.25f),
                            StandardDeviation = 0,
                        },
                        Type = BiomeType.Sand,
                        LeafPointsGenerator = new LeafPointsGenerator()
                    }
                });

            UnityEngine.Random.InitState(123);
            var newMaterial = new Material(Shader.Find("Custom/TerrainTextureTest2"));


            BiomeInstanceDetailGenerator detailGenerator =
                DebugDetailGenerator();

            var arrayGenerator2 = new StainTerrainArrayFromBiomesGenerator(container, detailGenerator,
                StainSpaceToUnitySpaceTranslator.DefaultTranslator,
                new StainTerrainArrayFromBiomesGeneratorConfiguration()
                {
                    TexturesSideLength = 64
                });

            //var arrayGenerator = new DummyStainTerrainArrayFromBiomesGenerator(container,
            //    new StainTerrainArrayFromBiomesGeneratorConfiguration()
            //    {
            //        TexturesSideLength = 256
            //    });
            var resourceGenerator = new ComputationStainTerrainResourceGenerator(
                new StainTerrainResourceComposer(
                    new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator())),
                new StainTerrainArrayMelder(),
                arrayGenerator2);
            var terrainResource = resourceGenerator.GenerateTerrainTextureDataAsync().Result;

            ConfigureMaterial(terrainResource, newMaterial);

            RenderTextureGameObject.GetComponent<MeshRenderer>().material = newMaterial;
        }

        public static BiomeInstanceDetailGenerator DebugSimpleGenerator()
        {
            return new BiomeInstanceDetailGenerator(new Dictionary<BiomeType, BiomeInstanceDetailTemplate>()
            {
                {
                    BiomeType.Grass, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    new Color(1f, 0, 0, 1),
                                    new Color(1f, 0, 0, 1),
                                    new Color(1f, 0, 0, 1),
                                    new Color(1f, 0, 0, 1),
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
                    BiomeType.Forest, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    new Color(0, 1f, 0, 1),
                                    new Color(0, 1f, 0, 1),
                                    new Color(0, 1f, 0, 1),
                                    new Color(0, 1f, 0, 1),
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
                    BiomeType.Sand, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    new Color(0, 0, 1, 1),
                                    new Color(0, 0, 1, 1),
                                    new Color(0, 0, 1, 1),
                                    new Color(0, 0, 1, 1),
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
            });
        }

        public static BiomeInstanceDetailGenerator DebugDetailGenerator()
        {
            return new BiomeInstanceDetailGenerator(GenerateDebugBiomeInstanceDetailTemplates());
        }

        public static Dictionary<BiomeType, BiomeInstanceDetailTemplate> GenerateDebugBiomeInstanceDetailTemplates()
        {
            return new Dictionary<BiomeType, BiomeInstanceDetailTemplate>()
            {
                {
                    BiomeType.Grass, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    new Color(0.6f, 0, 0, 1),
                                    new Color(0.4f, 0, 0, 1),
                                    new Color(0.2f, 0, 0, 1),
                                    new Color(0.1f, 0, 0, 1),
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
                    BiomeType.Forest, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    new Color(0.6f, 0.6f, 0, 1),
                                    new Color(0, 0.4f, 0, 1),
                                    new Color(0, 0.2f, 0.7f, 1),
                                    new Color(0, 0.1f, 0, 1),
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
                    BiomeType.Sand, new BiomeInstanceDetailTemplate()
                    {
                        ColorTemplate = new BiomeColorTemplate()
                        {
                            BaseColors = new List<ColorPack>()
                            {
                                new ColorPack(new[]
                                {
                                    new Color(0, 0, 0.6f, 1),
                                    new Color(0, 0, 0.4f, 1),
                                    new Color(0, 0, 0.2f, 1),
                                    new Color(0, 0, 0.1f, 1),
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

        public static void ConfigureMaterial(StainTerrainResource terrainResource, Material material)
        {
            material.SetTexture("_PaletteTex", terrainResource.TerrainPaletteTexture);
            material.SetTexture("_PaletteIndexTex", terrainResource.PaletteIndexTexture);
            material.SetTexture("_ControlTex", terrainResource.ControlTexture);
            material.SetFloat("_TerrainTextureSize", terrainResource.TerrainTextureSize);
            material.SetFloat("_PaletteMaxIndex", terrainResource.PaletteMaxIndex);
        }
    }
}