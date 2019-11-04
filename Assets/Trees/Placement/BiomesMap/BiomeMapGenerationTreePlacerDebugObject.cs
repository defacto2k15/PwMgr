using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.ShaderUtils;
using Assets.TerrainMat;
using Assets.Trees.Db;
using Assets.Trees.DesignBodyDetails;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Quadtree;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Trees.Placement.BiomesMap
{
    public class BiomeMapGenerationTreePlacerDebugObject : MonoBehaviour
    {
        private Vector2 WindowSize = new Vector2(10, 10);
        private float MaxElementsPerCellsSquareMeter = 10f / 100;
        private float MaxTriesPerCellsSquareMeter = 15f / 100;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            Recalculate();
        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.W))
            {
                Recalculate();
            }
        }

        private static Texture2D GenerateIntensityTexture()
        {
            TextureRenderer textureRenderer = new TextureRenderer();
            Texture2D dummyInputTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            UniformsPack uniforms = new UniformsPack();
            uniforms.SetUniform("_Scale", 3.5f);
            Texture2D intensityTexture = textureRenderer.RenderTexture("Custom/Misc/FbmNoiseGenerator",
                dummyInputTexture, uniforms,
                new RenderTextureInfo(1024, 1024, RenderTextureFormat.ARGB32),
                new ConventionalTextureInfo(1024, 1024, TextureFormat.RGBA32));
            return intensityTexture;
        }

        public void Recalculate()
        {
            List<VegetationSpeciesCreationCharacteristics> creationCharacteristicses =
                CreateSpeciesCreationCharacteristicses();

            var generationArea = new GenerationArea(0, 0, 200, 200);
            var positionRemapper = new PositionRemapper(generationArea);
            UnityEngine.Random.InitState(13);


            var intensityMap = new VegetationSpeciesIntensityMap(GenerateIntensityTexture(), positionRemapper);

            //////////////////////////////

            var heightTexture = CreateDebugHeightTexture();
            var biomesTree = CreateDebugBiomesTree();


            var biomesArrayGenerator = new VegetationBiomesArrayGenerator(
                new DebugTerrainHeightArrayProvider(heightTexture, new MyRectangle(0, 0, 1, 1)),
                biomesTree,
                new RandomFieldFigureGeneratorUTProxy(new Ring2RandomFieldFigureGenerator(
                    new TextureRenderer(), new Ring2RandomFieldFigureGeneratorConfiguration()
                    {
                        PixelsPerUnit = new Vector2(10, 10)
                    }
                )),
                new VegetationBiomesArrayGenerator.VegetationBiomesArrayGeneratorConfiguration()
                {
                    RandomHeightJitterRange = new Vector2(0.9f, 1.1f)
                }
            );

            var msw = new MyStopWatch();
            msw.StartSegment("GeneragingArray");

            var biomesMap = biomesArrayGenerator.Generate(
                TerrainCardinalResolution.MAX_RESOLUTION,
                new IntVector2(30, 30),
                new MyRectangle(0, 0, 90, 90));

            msw.StartSegment("Object placement!");
            var vegetationSubjectsDatabase = new DebugColoringVegetationSubjectsDatabase();

            var placer = new VegetationPlacer(vegetationSubjectsDatabase);
            placer.PlaceVegetation(
                areaToGenerate: generationArea,
                configuration: new VegetationPlacerConfiguration()
                {
                    GenerationWindowSize = WindowSize,
                    MaxTriesPerMaxElementsFactor = 1.8f
                },
                intensityMap: intensityMap,
                biomesMap: biomesMap,
                creationCharacteristicses: creationCharacteristicses,
                levelRank: VegetationLevelRank.Big,
                spotInfoProvider: new ConcievingTerrainSpotInfoProvider()
            );


            UnityEngine.Debug.Log("Generated in " + msw.CollectResults());

            //VegetationDatabaseFileUtils.WriteToFile("file1.json", vegetationSubjectsDatabase);
        }

        public static List<VegetationSpeciesCreationCharacteristics> CreateSpeciesCreationCharacteristicses(
            float sizeMultiplier = 1)
        {
            return new List
                <VegetationSpeciesCreationCharacteristics>()
                {
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree1A,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier,
                        //SpotDependentConcievingPropability todo start it
                        //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree2A,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier,
                        //SpotDependentConcievingPropability 
                        //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.6f), 0.1f))
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree3A,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier,
                        //SpotDependentConcievingPropability 
                        //    = new SlopeDependentConcievingPropability(new MarginedRange(new Vector2(0.3f, 0.95f), 0.1f))
                    },


                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree1B,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree2B,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree3B,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },

                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree1C,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree2C,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree3C,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree1D,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree2D,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree3D,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f) * sizeMultiplier
                    },
                };
        }

        private MyQuadtree<BiomeProviderAtArea> CreateDebugBiomesTree()
        {
            var tree = new MyQuadtree<BiomeProviderAtArea>();
            float perM2 = 15; // todo ile drzew na m2

            tree.Add(new BiomeProviderAtArea(
                MyNetTopologySuiteUtils.ToPolygon(new MyRectangle(0, 0, 90, 90)),
                new HeightDependentBiomeProvider(
                    new List<VegetationBiomeLevel>()
                    {
                        new VegetationBiomeLevel(
                            biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                            {
                                new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1A, 0.7f),
                                new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2A, 0.15f),
                                new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3A, 0.15f),
                            },
                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                            maxElementsPerCellPerM2: perM2 / 100f),
                    },
                    new MarginedRange(new Vector2(0, 0.5f), 0.3f)
                ),
                1
            ));

            tree.Add(new BiomeProviderAtArea(
                MyNetTopologySuiteUtils.ToPolygon(new MyRectangle(0, 0, 90, 90)),
                new HeightDependentBiomeProvider(
                    new List<VegetationBiomeLevel>()
                    {
                        new VegetationBiomeLevel(
                            biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                            {
                                new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1B, 0.7f),
                                new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2B, 0.15f),
                                new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3B, 0.15f),
                            },
                            exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                            maxElementsPerCellPerM2: perM2 / 100f),
                    },
                    new MarginedRange(new Vector2(0.5f, 1f), 0.15f)
                ),
                1
            ));

            return tree;
        }

        private Texture2D CreateDebugHeightTexture()
        {
            var newTexture = new Texture2D(241, 241, TextureFormat.ARGB32, false, false);
            for (int x = 0; x < 241; x++)
            {
                for (int y = 0; y < 241; y++)
                {
                    var color = new Color(x / 241f, 0, 0, 0);
                    newTexture.SetPixel(x, y, color);
                }
            }
            return newTexture;
        }
    }
}