using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.TerrainMat
{
    class TerrainMaterialDebug : MonoBehaviour
    {
        public GameObject RenderTextureGameObject;

        private void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            UnityEngine.Random.InitState(123);
            var newMaterial = new Material(Shader.Find("Custom/TerrainTextureTest2"));

            var biomeGenerator = new DebugBiomeContainerGenerator();
            var biomesInfo = biomeGenerator.GenerateBiomesContainer(new BiomesContainerConfiguration()
            {
                Center = new Vector2(0.5f, 0.5f),
                HighQualityQueryDistance = 0.3f
            });
            var arrayGenerator =
                new DummyStainTerrainArrayFromBiomesGenerator(biomesInfo,
                    new StainTerrainArrayFromBiomesGeneratorConfiguration());
            var resourceGenerator = new ComputationStainTerrainResourceGenerator(
                new StainTerrainResourceComposer(
                    new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator())),
                new StainTerrainArrayMelder(),
                arrayGenerator);
            var terrainResource = resourceGenerator.GenerateTerrainTextureDataAsync().Result;

            ConfigureMaterial(terrainResource, newMaterial);

            //var newMaterial = new Material(Shader.Find("Custom/TerrainTextureBombing"));
            ////var bombTexture = SavingFileManager.LoadPngTextureFromFile("Assets/textures/dot3.png", 256,256, TextureFormat.RGBA32);
            ////newMaterial.SetTexture("_BombsTex", bombTexture);
            //var bombTerrainTexture = new BombTerrainTexture();
            //bombTerrainTexture.ConfigureMaterial(newMaterial);

            RenderTextureGameObject.GetComponent<MeshRenderer>().material = newMaterial;
        }

        private void ConfigureMaterial(StainTerrainResource terrainResource, Material material)
        {
            material.SetTexture("_PaletteTex", terrainResource.TerrainPaletteTexture);
            material.SetTexture("_PaletteIndexTex", terrainResource.PaletteIndexTexture);
            material.SetTexture("_ControlTex", terrainResource.ControlTexture);
            material.SetFloat("_TerrainTextureSize", terrainResource.TerrainTextureSize);
            material.SetFloat("_PaletteMaxIndex", terrainResource.PaletteMaxIndex);
        }
    }

    public class BombTerrainTexture
    {
        public void ConfigureMaterial(Material material)
        {
            var singleBombTexturePaths = new List<String>
            {
                "Assets/textures/dot2.png",
                "Assets/textures/dot3.png",
                "Assets/textures/dot4.png",
                "Assets/textures/dot5.png",
                "Assets/textures/dot6.png",

                "Assets/textures/dot2.png",
                "Assets/textures/dot2.png",
                "Assets/textures/dot2.png",
                "Assets/textures/dot3.png",
                "Assets/textures/dot4.png",
                "Assets/textures/dot5.png",
                "Assets/textures/dot6.png",
                "Assets/textures/dot3.png",
                "Assets/textures/dot4.png",
                "Assets/textures/dot5.png",
                "Assets/textures/dot6.png",
                "Assets/textures/dot3.png",
                "Assets/textures/dot4.png",
                "Assets/textures/dot5.png",
                "Assets/textures/dot6.png",
            };

            var bombsCount = singleBombTexturePaths.Count;

            var oneTextureLength = 256;
            var maxTextureLength = 2048;
            var maxTexturesOneLine = maxTextureLength / oneTextureLength;

            int xBombsCount = Math.Min(maxTexturesOneLine, bombsCount);
            int yBombsCount = (int) Math.Ceiling((double) bombsCount / maxTexturesOneLine);

            var outTexture = new Texture2D(xBombsCount * oneTextureLength, yBombsCount * oneTextureLength,
                TextureFormat.RGBA32, true);

            var allBombs = singleBombTexturePaths.Select(path => LoadBombTexture(path, oneTextureLength)).ToList();

            for (int xBombIndex = 0; xBombIndex < xBombsCount; xBombIndex++)
            {
                for (int yBombIndex = 0; yBombIndex < yBombsCount; yBombIndex++)
                {
                    int bombTextureIdx = (xBombIndex + yBombIndex * xBombsCount) % bombsCount;
                    var bombTexture = allBombs[bombTextureIdx];
                    for (int x = 0; x < oneTextureLength; x++)
                    {
                        for (int y = 0; y < oneTextureLength; y++)
                        {
                            outTexture.SetPixel(x + xBombIndex * oneTextureLength, y + yBombIndex * oneTextureLength,
                                bombTexture.GetPixel(x, y));
                        }
                    }
                }
            }
            outTexture.Apply();

            material.SetTexture("_BombsTex", outTexture);
            material.SetVector("_BombsTexCount", new Vector4(xBombsCount / 64f, yBombsCount / 64f, 0, 0));
        }

        private Texture2D LoadBombTexture(string path, int oneTextureLength)
        {
            return SavingFileManager.LoadPngTextureFromFile(path, oneTextureLength, oneTextureLength,
                TextureFormat.RGBA32, false);
        }
    }
}