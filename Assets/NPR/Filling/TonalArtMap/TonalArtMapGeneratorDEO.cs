using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using Assets.Utils.MT;
using ImageProcessor.Imaging.Formats;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Assets.NPRResources.TonalArtMap
{
    public class TonalArtMapGeneratorDEO : MonoBehaviour
    {
        public void Start()
        {
            StartDiagram();
        }

        public void StartDiagram()
        {
            var tonesCount = 4;
            var levelsCount = 4;

            TaskUtils.SetGlobalMultithreading(false);

            var tones = TAMTone.GenerateList(tonesCount, 3, 4);
            var levels = TAMMipmapLevel.CreateList(levelsCount);

            var tamPackGenerator = new TAMPackGenerator();
            var msw = new MyStopWatch();
            msw.StartSegment("Gen");
            var pack = tamPackGenerator.GenerateTamPack(GenerateDiagramConfiguration(tones, levels,0.7f), true, Object.FindObjectOfType<ComputeShaderContainerGameObject>());
            Debug.Log(msw.CollectResults());
            var fileManager = new TAMPackFileManager();
            fileManager.Save(@"C:\mgr\tmp\tam2\", tones, levels, pack);
        }

        public static TAMPackGenerationConfiguration GenerateDiagramConfiguration(List<TAMTone> tones, List<TAMMipmapLevel> levels, float exclusionZoneMultiplier)
        {
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
                    oneToneDir[levels[levelIndex]] = TAMPackGenerationConfiguration.CalculateExclusionZoneValue(exclusionZoneMultiplier*(toneIndex + levelIndex));
                }
                exclusionZoneValues[tones[toneIndex]] = oneToneDir;
            }

            return new TAMPackGenerationConfiguration()
            {
                PoissonTamImageDiagramGeneratorConfiguration = new PoissonTAMImageDiagramGeneratorConfiguration()
                {
                    ExclusionZoneValues = exclusionZoneValues ,
                    GenerationCount = 60
                }
                , StrokesGeneratorConfiguration = new StrokesGeneratorConfiguration()
                    {
                        StrokeLengthRange = new Vector2(0.25f, 0.4f),
                        StrokeLengthJitterRange = new Vector2(0f, 0.1f),
                        StrokeHeightRange = new Vector2(0.001f, 0.003f),
                        StrokeHeightJitterRange = new Vector2(0.0000f, 0.0005f),
                        MaxRotationJitter = Mathf.PI * 0.08f
                    }
                , BlankImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\blank.png"
                , StrokeImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\Stroke1.png"
                , Margin = 0.25f
                , SmallestLevelSoleImageResolution = new IntVector2(256,256)
                , RendererOneStepSize = new Vector2(100,100)
                , Tones = tones
                , Levels = levels
                , StrokeHeightMultiplierForZeroLevel = 16
            };
        }



        public void Start0()
        {
            var tonesCount = 5;
            var levelsCount = 5;
            TaskUtils.SetGlobalMultithreading(false);
            var tones = TAMTone.CreateList(tonesCount, new Dictionary<int, TAMStrokeOrientation>()
            {
                {0,TAMStrokeOrientation.Horizontal},
                {3,TAMStrokeOrientation.Vertical },
                {5,TAMStrokeOrientation.Both }
            });
            var levels = TAMMipmapLevel.CreateList(levelsCount);

            var fileManager = new TAMPackFileManager();
            var soleImagesPack = fileManager.Load(@"C:\mgr\tmp\tam1\", tones, levels );

            var generator = new TAMArrayGenerator();
            var tex2DArray = generator.Generate(soleImagesPack, tones, levels);

            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var material = new Material(Shader.Find("Custom/Debug/TextureArrayLod"));
            material.SetTexture("_MainTex", tex2DArray);
            go.GetComponent<MeshRenderer>().material = material;
            MyAssetDatabase.CreateAndSaveAsset(tex2DArray, "Assets/Generated/TAM1.asset");
        }

        public void SetMaterialRenderingModeToAlphablend(Material m)
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
}
