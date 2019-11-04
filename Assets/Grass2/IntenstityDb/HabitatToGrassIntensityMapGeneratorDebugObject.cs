using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Grass2.Types;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Roads.Engraving;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Grass2.IntenstityDb
{
    class HabitatToGrassIntensityMapGeneratorDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public static int HabitatTexturesSize = 512;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var generator = new HabitatToGrassIntensityMapGenerator(ComputeShaderContainer,
                new UnityThreadComputeShaderExecutorObject(), new CommonExecutorUTProxy(),
                new HabitatToGrassIntensityMapGenerator.HabitatToGrassIntensityMapGeneratorConfiguration()
                {
                    GrassTypeToSourceHabitats = new Dictionary<GrassType, List<HabitatType>>()
                    {
                        {GrassType.Debug1, new List<HabitatType>() {HabitatType.Forest}},
                        {GrassType.Debug2, new List<HabitatType>() {HabitatType.Meadow, HabitatType.Fell}},
                    },
                    OutputPixelsPerUnit = 2
                });

            var result = generator.GenerateGrassIntenstiyAsync(
                new MyRectangle(0, 0, 256, 256),
                CreateHabitatTexturesDict(),
                new IntVector2(HabitatTexturesSize, HabitatTexturesSize),
                GeneratePathProximityTexture()
            ).Result;

            Debug.Log("ResultCount: " + result.Count);
            Debug.Log("A1: " + result[0].IntensityFigure.GetPixel(0, 0));

            result.Select((c, i) =>
            {
                CreateDebugObject(c, i);
                return 0;
            }).ToList();
        }

        private UvdSizedTexture GeneratePathProximityTexture()
        {
            var texSize = new IntVector2(241, 241);
            var tex = new Texture2D(texSize.X, texSize.Y, TextureFormat.ARGB32, false, true);
            for (int x = 0; x < texSize.X; x++)
            {
                for (int y = 0; y < texSize.Y; y++)
                {
                    var distanceToAxe = Mathf.Abs(y - 40) / 2;
                    if (distanceToAxe > 5)
                    {
                        tex.SetPixel(x, y, new Color(1, 1, 1, 1));
                    }
                    else
                    {
                        var encodedProximity = PathProximityUtils.EncodeProximity(distanceToAxe, 5);
                        tex.SetPixel(x, y, new Color(encodedProximity.x, encodedProximity.y, 0, 0));
                    }
                }
            }
            tex.Apply();

            return new UvdSizedTexture()
            {
                TextureWithSize = new TextureWithSize()
                {
                    Size = texSize,
                    Texture = tex
                },
                Uv = new MyRectangle(0, 0, 1, 1)
            };
        }

        private Dictionary<HabitatType, Texture2D> CreateHabitatTexturesDict()
        {
            var size = HabitatTexturesSize;
            var forrestTexture = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            forrestTexture.wrapMode = TextureWrapMode.Clamp;
            var meadowTexture = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            meadowTexture.wrapMode = TextureWrapMode.Clamp;
            var fellTexture = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            fellTexture.wrapMode = TextureWrapMode.Clamp;

            var allTexture = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            allTexture.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var forrestVal = Mathf.InverseLerp(0.6f, 0.8f, x / (float) HabitatTexturesSize);
                    forrestTexture.SetPixel(x, y, new Color(forrestVal, 0, 0, 0));

                    var meadowVal = Mathf.InverseLerp(0.2f, 0.4f, y / (float) HabitatTexturesSize);
                    meadowTexture.SetPixel(x, y, new Color(meadowVal, 0, 0, 0));

                    var fellVal = Mathf.InverseLerp(0.1f, 0.0f, y / (float) HabitatTexturesSize);
                    fellTexture.SetPixel(x, y, new Color(fellVal, 0, 0, 0));

                    allTexture.SetPixel(x, y, new Color(forrestVal, meadowVal, fellVal, 1));
                }
            }
            allTexture.Apply(false);
            SavingFileManager.SaveTextureToPngFile($@"C:\inz2\all.png", allTexture);

            return new Dictionary<HabitatType, Texture2D>()
            {
                {HabitatType.Forest, forrestTexture},
                {HabitatType.Meadow, meadowTexture},
                {HabitatType.Fell, fellTexture},
            };
        }

        public static Texture CreateTextureFrom(IntensityFieldFigure intensityFigure)
        {
            int width = intensityFigure.Width;
            int height = intensityFigure.Height;

            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, new Color(intensityFigure.GetPixel(x, y), 0, 0));
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false);

            SavingFileManager.SaveTextureToPngFile($@"C:\inz2\tex{texNo++}.png", tex);
            return tex;
        }

        private static int texNo = 0;

        public static void CreateDebugObject(Grass2TypeWithIntensity typeWithIntensity, int i)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = typeWithIntensity.GrassType.ToString();
            go.transform.localPosition = new Vector3(i * 1.2f, 0, 0);

            var texture = CreateTextureFrom(typeWithIntensity.IntensityFigure);
            go.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", texture);
            texture.wrapMode = TextureWrapMode.Clamp;
        }
    }
}