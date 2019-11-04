using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class Multipass1ErosionSimulationDebugObject : MonoBehaviour
    {
        public Texture OutputTexture;
        public GameObject OutputShowingObject;

        public void Start()
        {
            GenerateTexture();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                GenerateTexture();
            }
        }

        private void GenerateTexture()
        {
            var heightTexture1 = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\temp3.png", 240,
                240, TextureFormat.RGBA32, true, true);
            Material material = new Material(Shader.Find("Custom/TerGen/ErosionThermal"));
            material.SetTexture("_MainInputTex", heightTexture1);

            RenderTextureInfo renderTextureInfo = new RenderTextureInfo(240, 240, RenderTextureFormat.ARGB32);

            ConventionalTextureInfo outTextureInfo = new ConventionalTextureInfo(240, 240, TextureFormat.ARGB32, false);

            OutputTexture = UltraTextureRenderer.RenderTextureAtOnce(material, renderTextureInfo, outTextureInfo);
            OutputShowingObject.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", OutputTexture);
        }
    }
}