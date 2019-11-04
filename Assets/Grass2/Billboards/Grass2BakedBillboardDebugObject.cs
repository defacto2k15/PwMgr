using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Grass2.Billboards
{
    public class Grass2BakedBillboardDebugObject : MonoBehaviour
    {
        public Texture TextureA;
        public Texture TextureB;
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var fileManager = new Grass2BillboardClanFilesManager();
            var singleClan = fileManager.Load(@"C:\inz\billboards\", new IntVector2(256, 256));
            var smallSingleClan = new Grass2SingledBillboardClan()
            {
                BillboardsList = new List<DetailedGrass2SingleBillboard>()
                {
                    singleClan.BillboardsList[0]
                }
            };

            var duoGenerator =
                new Grass2BakingBillboardClanGenerator(ComputeShaderContainer,
                    new UnityThreadComputeShaderExecutorObject());

            var clan2 = duoGenerator.GenerateBakedAsync(singleClan).Result;

            var texArray = new Texture2DArray(16, 16, 4, TextureFormat.ARGB32, false);
            var colorsArray = new Color[16 * 16];
            for (int i = 0; i < 16 * 16; i++)
            {
                colorsArray[i] = Color.blue;
            }
            for (int i = 0; i < 4; i++)
            {
                texArray.SetPixels(colorsArray, i);
            }
            texArray.Apply(false);


            var material = new Material(Shader.Find("Custom/Debug/TextureArray"));
            var ax = clan2.BladeSeedTextureArray;
            material.SetTexture("_ArrayTex", clan2.BladeSeedTextureArray);
            //material.SetTexture("_ArrayTex", Grass2DuoBillboardClanGenerator.GLOB_TEX);
            //material.SetTexture("_MainTex", Grass2DuoBillboardClanGenerator.GLOB_TEX);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.GetComponent<MeshRenderer>().material = material;

            TextureA = clan2.DetailTextureArray;
            TextureB = clan2.BladeSeedTextureArray;
        }
    }
}