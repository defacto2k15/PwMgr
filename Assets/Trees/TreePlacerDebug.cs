using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.Creator;
using Assets.ShaderUtils;
using Assets.Trees.Db;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement;
using Assets.Trees.Placement.BiomesMap;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Trees
{
    public class TreePlacerDebug : MonoBehaviour
    {
        private Vector2 WindowSize = new Vector2(10, 10);
        private float MaxElementsPerCellsSquareMeter = 10f / 100;
        private float MaxTriesPerCellsSquareMeter = 15f / 100;

        public void Start()
        {
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
            MyProfiler.BeginSample("TreePlacerDebugRecalculate");
            List<VegetationSpeciesCreationCharacteristics> creationCharacteristicses = new List
                <VegetationSpeciesCreationCharacteristics>()
                {
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree1A,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree2A,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree3A,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },


                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree1B,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree2B,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree3B,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },


                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree1C,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree2C,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },
                    new VegetationSpeciesCreationCharacteristics()
                    {
                        CurrentVegetationType = VegetationSpeciesEnum.Tree3C,
                        ExclusionRadius = 1f,
                        MaxCreationRadius = 5,
                        MaxSpeciesOccurenceDistance = 5f,
                        InitialSpeciesOccurencePropability = 0.6f,
                        SizeMultiplierFactorRange = new Vector2(0.8f, 1.2f)
                    },
                };

            var generationArea = new GenerationArea(0, 0, 200, 200);
            var positionRemapper = new PositionRemapper(generationArea);
            UnityEngine.Random.InitState(13);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var intensityMap = new VegetationSpeciesIntensityMap(GenerateIntensityTexture(), positionRemapper);

            SavingFileManager fileManager = new SavingFileManager();

            Texture2D biomesTexture =
                SavingFileManager.LoadPngTextureFromFile("Assets/biomeMap1.png", 64, 64, TextureFormat.RGBA32, false);
            float perM2 = 15; // todo ile drzew na m2
            var biomesMap = new FromTextureVegetationBiomesMap(
                biomeLevels: new List<VegetationBiomeLevel>()
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
                    new VegetationBiomeLevel(
                        biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                        {
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1B, 0.7f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2B, 0.15f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3B, 0.15f),
                        },
                        exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                        maxElementsPerCellPerM2: perM2 / 100f),
                    new VegetationBiomeLevel(
                        biomeOccurencePropabilities: new List<VegetationSpeciesOccurencePropability>()
                        {
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree1C, 0.7f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree2C, 0.15f),
                            new VegetationSpeciesOccurencePropability(VegetationSpeciesEnum.Tree3C, 0.15f),
                        },
                        exclusionRadiusRange: new Vector2(0.3f, 0.7f),
                        maxElementsPerCellPerM2: perM2 / 100f)
                },
                biomesTexture: biomesTexture,
                remapper: positionRemapper
            );


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

            // now smaller 
            //var scaledCreationCharacteristics = scaleSize(creationCharacteristicses, 0.7f);
            //placer.PlaceVegetation(
            //    areaToGenerate: new GenerationArea(0, 0, 200, 200),
            //    configuration: new VegetationPlacerConfiguration()
            //        {
            //            GenerationWindowSize = WindowSize,
            //            MaxTriesPerMaxElementsFactor = 1.8f
            //        },
            //    intensityMap: intensityMap,
            //    biomesMap: biomesMap.ScaleMaxElementsPerCellPerM2(2),
            //    creationCharacteristicses: scaledCreationCharacteristics);

            sw.Stop();
            UnityEngine.Debug.Log("Generated in " + sw.ElapsedMilliseconds);
            MyProfiler.EndSample();

            VegetationDatabaseFileUtils.WriteToFile("file1.json", vegetationSubjectsDatabase);
        }

        private List<VegetationSpeciesCreationCharacteristics> scaleSize(
            List<VegetationSpeciesCreationCharacteristics> creationCharacteristicses, float scale)
        {
            return creationCharacteristicses.Select(c => c.ScaleSizeMultiplierFactorRange(scale)).ToList();
        }
    }
}