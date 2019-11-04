using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMPackGenerator
    {
        public TAMSoleImagesPack GenerateTamPack( TAMPackGenerationConfiguration configuration, bool generateDebugPlates, ComputeShaderContainerGameObject shaderContainerGameObject)
        {
            var tones = configuration.Tones;
            var levels = configuration.Levels;
            var tonesCount = tones.Count;
            var levelsCount = levels.Count;
            var templateGenerator = new TAMTemplateGenerator(
                new PoissonTAMImageDiagramGenerator(
                    new TAMPoissonDiskSampler(),
                    new StrokesGenerator(configuration.StrokesGeneratorConfiguration),
                    configuration.PoissonTamImageDiagramGeneratorConfiguration)
                );

            var template = templateGenerator.Generate(new TAMTemplateSpecification()
            {
                Tones = tones,
                MipmapLevels = levels
            });

            var margin = configuration.Margin;
            var smallestLevelSoleImageResolution = configuration.SmallestLevelSoleImageResolution;

            var renderer = new TAMDeckRenderer(
                Image.FromFile(configuration.StrokeImagePath),
                Image.FromFile(configuration.BlankImagePath),
                new TAMDeckRendererConfiguration()
                {
                    SoleImagesResolutionPerLevel = levels
                        .Select((level, i) => new {level, i})
                        .ToDictionary(c => c.level, c => c.i)
                        .ToDictionary(pair => pair.Key, pair => (smallestLevelSoleImageResolution * Mathf.Pow(2, pair.Value)).ToIntVector()),
                    Margin = margin,
                    StrokeHeightMultiplierPerLevel = levels
                        .Select((level, i) => new {level, i})
                        .ToDictionary(c => c.level, c => c.i)
                        .ToDictionary(pair => pair.Key, pair => configuration.StrokeHeightMultiplierForZeroLevel/Mathf.Pow(2, pair.Value))
                });
            var deck = renderer.Render(template);

            var wrapper = new TAMMarginsWrapper(new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(shaderContainerGameObject),
                new TextureRendererServiceConfiguration()
                {
                    StepSize = configuration.RendererOneStepSize
                })), new TAMMarginsWrapperConfiguration()
            {
                Margin = margin,
            });

            for (int toneIndex = 0; toneIndex < tonesCount; toneIndex++)
            {
                for (int levelIndex = 0; levelIndex < levelsCount; levelIndex++)
                {
                    var image = deck.Columns[tones[toneIndex]][levels[levelIndex]];
                    var soleTexture = wrapper.WrapTexture(TAMUtils.ImageToTexture2D(image));
                    var strokesCount = template.Columns[tones[toneIndex]][levels[levelIndex]].Strokes.Count;
                    if (generateDebugPlates)
                    {
                        CreateTexturedPlate(soleTexture, 11f * new Vector2(toneIndex, levelIndex),
                            "Tone " + toneIndex + " Level " + levelIndex + " Count" + strokesCount);
                    }
                }
            }

            var soleImagesPack = new TAMSoleImagesPack(deck.Columns.ToDictionary(
                c => c.Key,
                c => c.Value.ToDictionary(
                    k => k.Key,
                    k => wrapper.WrapTexture(TAMUtils.ImageToTexture2D(k.Value)))));
            return soleImagesPack;
        }

        private void SetMaterialRenderingModeToAlphablend(Material m)
        {
            m.SetFloat("_Mode", 2);
            m.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }

        private void CreateTexturedPlate(Texture tex, Vector2 position, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.GetComponent<MeshRenderer>().material.mainTexture = tex;
            SetMaterialRenderingModeToAlphablend(go.GetComponent<MeshRenderer>().material);
            go.transform.position = new Vector3(position.x, 0, position.y);
            go.name = name;
        }
    }


    public class TAMPackGenerationConfiguration
    {
        public StrokesGeneratorConfiguration StrokesGeneratorConfiguration;
        public PoissonTAMImageDiagramGeneratorConfiguration PoissonTamImageDiagramGeneratorConfiguration;
        public string StrokeImagePath;
        public string BlankImagePath;
        public float Margin;
        public IntVector2 SmallestLevelSoleImageResolution;
        public Vector2 RendererOneStepSize;
        public List<TAMMipmapLevel> Levels;
        public List<TAMTone> Tones;
        public float StrokeHeightMultiplierForZeroLevel;

        public static TAMPackGenerationConfiguration GetDefaultConfiguration(List<TAMTone> tones, List<TAMMipmapLevel> levels,
            float exclusionZoneMultiplier, string strokeImagePath, string blankImagePath)
        {
            // exclusionZoneMultiplier - lower values - less lines
            // for Exclusion zone values i use function computed from applying cubic regression on hand-picked values. Old version should be in git 6-14-2019
            // Tone0 Level0 has x=0
            // Tone0 LevelN has x=N
            // ToneM LevelN has x = M+N 

            var exclusionZoneValues = new Dictionary<TAMTone, Dictionary<TAMMipmapLevel, float>>();
            for (int toneIndex = 0; toneIndex < tones.Count; toneIndex++)
            {
                var oneToneDir = new Dictionary<TAMMipmapLevel, float>();
                for (var levelIndex = 0; levelIndex < levels.Count; levelIndex++)
                {
                    oneToneDir[levels[levelIndex]] = CalculateExclusionZoneValue(exclusionZoneMultiplier*(toneIndex + levelIndex));
                }
                exclusionZoneValues[tones[toneIndex]] = oneToneDir;
            }

            return new TAMPackGenerationConfiguration()
            {
                PoissonTamImageDiagramGeneratorConfiguration = new PoissonTAMImageDiagramGeneratorConfiguration()
                {
                    ExclusionZoneValues = exclusionZoneValues ,
                    GenerationCount = 120
                }
                , StrokesGeneratorConfiguration = new StrokesGeneratorConfiguration()
                    {
                        StrokeLengthRange = new Vector2(0.05f, 0.2f),
                        StrokeLengthJitterRange = new Vector2(0f, 0.04f),
                        StrokeHeightRange = new Vector2(0.001f, 0.003f),
                        StrokeHeightJitterRange = new Vector2(0.0000f, 0.0005f),
                        MaxRotationJitter = Mathf.PI * 0.08f,
                        PerMipmapLevelHeightMultiplier = 0.8f
                    }
                , BlankImagePath = blankImagePath
                , StrokeImagePath = strokeImagePath
                , Margin = 0.25f
                , SmallestLevelSoleImageResolution = new IntVector2(64,64)
                , RendererOneStepSize = new Vector2(100,100)
                , Tones = tones
                , Levels = levels
                , StrokeHeightMultiplierForZeroLevel = 16
            };
        }

        public static float CalculateExclusionZoneValue(float x)
        {
            var value = (float) (0.2960525 - 0.09820618 * x + 0.01248135 * x * x - 0.0005781987 * x * x * x);
            var epsilon = 0.001f;
            if (value <= epsilon)
            {
                Debug.LogWarning($"While  CalculateExclusionZoneValue for x:{x} value was {value} that is lower than epsilon {epsilon}. Making it bigger");
                return epsilon;
            }

            return value;
        }
    }

}