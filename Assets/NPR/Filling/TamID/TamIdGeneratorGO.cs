using Assets.NPRResources.TonalArtMap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using ImageProcessor;
using ImageProcessor.Imaging;
using UnityEngine;
using Color = System.Drawing.Color;

namespace Assets.NPR.TamID
{
    public class TamIdGeneratorGO : MonoBehaviour
    {
        public bool SaveToFile = false;
        public String AssetName;
        public bool SmoothAlpha;
        public string TemporaryImagesPath = @"C:\mgr\tmp\tamid1\";
        public string StrokeImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\Stroke1WithT5.png";
        public string BlankImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\blank.png";
        public float StrokeGenerationConfigurationHeightScale = 1;
        public float StrokeGenerationConfigurationLengthScale = 1;
        public float ExclusionZoneMultiplier = 0.8f;

        public void Start()
        {
            var sw = new MyStopWatch();
            sw.StartSegment("TamIdGeneration");
            TaskUtils.SetGlobalMultithreading(false);
            var tonesCount = 5;
            var levelsCount = 5;
            var layersCount = 1;

            var tamTones = TAMTone.CreateList(tonesCount, new Dictionary<int, TAMStrokeOrientation>()
            {
                {0,TAMStrokeOrientation.Horizontal},
                {3,TAMStrokeOrientation.Vertical },
                {5,TAMStrokeOrientation.Both }
            });
            var tamMipmapLevels = TAMMipmapLevel.CreateList(levelsCount);

            var configuration = TamIdPackGenerationConfiguration
                .GetDefaultTamIdConfiguration(tamTones, tamMipmapLevels, ExclusionZoneMultiplier, layersCount, StrokeImagePath, BlankImagePath);
            ScaleStrokesGenerationConfiguration(configuration);
            configuration.UseSmoothAlpha = SmoothAlpha;

            var packGenerator = new TamIdPackGenerator();
            var pack = packGenerator.GenerateTamPack(configuration, false, FindObjectOfType<ComputeShaderContainerGameObject>());
            DrawDebugPlates(pack);
            var fileManager = new TamIdPackFileManager();
            Debug.Log("Sw: "+sw.CollectResults());

            if (SaveToFile)
            {
                fileManager.Save(TemporaryImagesPath, tamTones, tamMipmapLevels, layersCount, pack);
                var generator = new TamIdArrayGenerator();
                var tex2DArray = generator.Generate(pack, tamTones, tamMipmapLevels, layersCount);
                tex2DArray.wrapMode = TextureWrapMode.Repeat;
                if (SmoothAlpha)
                {
                    tex2DArray.filterMode = FilterMode.Trilinear;
                }
                MyAssetDatabase.CreateAndSaveAsset(tex2DArray, $"Assets/Generated/{AssetName}.asset");
            }
        }

        private void ScaleStrokesGenerationConfiguration(TamIdPackGenerationConfiguration configuration)
        {
            configuration.StrokesGeneratorConfiguration.StrokeHeightJitterRange *= StrokeGenerationConfigurationHeightScale;
            configuration.StrokesGeneratorConfiguration.StrokeHeightRange *= StrokeGenerationConfigurationHeightScale;
            configuration.StrokesGeneratorConfiguration.StrokeLengthJitterRange *= StrokeGenerationConfigurationLengthScale;
            configuration.StrokesGeneratorConfiguration.StrokeLengthRange *= StrokeGenerationConfigurationLengthScale;
        }

        public static void DrawDebugPlates(TamIdSoleImagesPack pack)
        {
            var toneIndex = 0;
            foreach (var toneColumn in pack.Columns)
            {
                var levelIndex = 0;
                foreach (var levelPair in toneColumn.Value)
                {
                    var layerIndex = 0;
                    foreach (var layer in levelPair.Value)
                    {
                        CreateTexturedPlate(layer, 11f * new Vector2(toneIndex, levelIndex), "Tone " + toneIndex + " Level " + levelIndex+" Layer "+layerIndex);
                        layerIndex++;
                    }

                    levelIndex++;
                }

                toneIndex++;
            }
        }

        public static void SetMaterialRenderingModeToAlphablend(Material m)
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

        public static void CreateTexturedPlate(Texture tex, Vector2 position, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.GetComponent<MeshRenderer>().material.mainTexture = tex;
            SetMaterialRenderingModeToAlphablend(go.GetComponent<MeshRenderer>().material);
            go.transform.position = new Vector3(position.x, 0, position.y);
            go.name = name;
        }
    }

}
