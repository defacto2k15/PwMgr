using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.TerrainMat;
using Assets.TerrainMat.Stain;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class Ring1SurfaceDebugObject : MonoBehaviour
    {
        public GameObject Surface;

        public void Start()
        {
            var oca = new Color[]
            {
                new Color(1, 0, 0), //0
                new Color(1, 1, 0), //1
                new Color(0, 1, 0), //2
                new Color(0, 0, 1), //3
                new Color(0, 1, 1), //4
                new Color(1, 0.5f, 0), //5
                new Color(1, 0, 1), //6
                new Color(0, 0, 0), //7
            };

            var colorPackList = new List<ColorPack>()
            {
                new ColorPack(new[] {oca[0], oca[1], oca[2], oca[3]}),
                new ColorPack(new[] {oca[0], oca[1], oca[2], oca[4]}),
                new ColorPack(new[] {oca[0], oca[1], oca[5], oca[4]}),
                new ColorPack(new[] {oca[0], oca[6], oca[5], oca[4]}),
                new ColorPack(new[] {oca[7], oca[6], oca[5], oca[4]}),
                //new ColorPack(new[] {oca[7], oca[6], oca[5], oca[4]}),
                //new ColorPack(new[] {oca[7], oca[6], oca[5], oca[4]}),
            };

            var paletteTex = new Texture2D(colorPackList.Count * 4, 1, TextureFormat.RGB24, false);
            for (int i = 0; i < colorPackList.Count; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    paletteTex.SetPixel(i * 4 + j, 0, colorPackList[i][j]);
                }
            }
            paletteTex.Apply();
            paletteTex.wrapMode = TextureWrapMode.Clamp;
            paletteTex.filterMode = FilterMode.Point;


            var indexes = new List<int>()
            {
                0,
                0,
                1,
                1,
                2,
                2,
                3,
                3,
                4,
                4,
                5,
                5
            };
            var paletteIndexTex = new Texture2D(10, 10, TextureFormat.RHalf, false);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    paletteIndexTex.SetPixel(x, y, new Color(Mathf.Floor(x / 2f) / 5f, 0, 0, 0));
                }
            }
            paletteIndexTex.wrapMode = TextureWrapMode.Clamp;
            paletteIndexTex.filterMode = FilterMode.Point;
            paletteIndexTex.Apply();

            var weights = new List<Vector4>()
            {
                new Vector4(1, 1, 1, 1),
                new Vector4(1, 1, 1, 0),
                new Vector4(1, 1, 1, 1),
                new Vector4(1, 1, 0, 1),
                new Vector4(1, 1, 1, 1),
                new Vector4(1, 0, 1, 1),
                new Vector4(1, 1, 1, 1),
                new Vector4(0, 1, 1, 1),
                new Vector4(1, 1, 1, 1),
            };

            var weightsX = new List<Vector3>()
            {
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 1f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(1f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
            };

            var controlTex = new Texture2D(10, 10, TextureFormat.RGB24, false);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    //controlTex.SetPixel(x, y, StainTerrainResourceComposer.PackControlValue(weights[x / 2]));
                    var col = weightsX[x];
                    controlTex.SetPixel(x, y, new Color(col.x, col.y, col.z));
                }
            }
            controlTex.wrapMode = TextureWrapMode.Clamp;
            controlTex.filterMode = FilterMode.Trilinear;
            controlTex.Apply();

            var material = new Material(Shader.Find("Custom/Terrain/Ring1"));

            ConfigureMaterial(new StainTerrainResource()
            {
                ControlTexture = controlTex,
                PaletteIndexTexture = paletteIndexTex,
                TerrainPaletteTexture = paletteTex,
                PaletteMaxIndex = 5,
                TerrainTextureSize = 10
            }, material);

            Surface.GetComponent<MeshRenderer>().material = material;

            SavingFileManager.SaveTextureToPngFile($@"C:\inz2\controlTex.png", controlTex);
            SavingFileManager.SaveTextureToPngFile($@"C:\inz2\paletteIndex.png", paletteIndexTex);
            SavingFileManager.SaveTextureToPngFile($@"C:\inz2\paletteTex.png", paletteTex);
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