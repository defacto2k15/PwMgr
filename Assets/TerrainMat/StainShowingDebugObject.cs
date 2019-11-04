using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class StainShowingDebugObject : MonoBehaviour
    {
        public GameObject RenderTextureGameObject;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var fileManager = new StainTerrainResourceFileManager(@"C:\inz\ring1\", new CommonExecutorUTProxy());

            var resource = fileManager.LoadResources().Result;

            var newMaterial = new Material(Shader.Find("Custom/TerrainTextureTest2"));
            BiomeGenerationDebugObject.ConfigureMaterial(resource, newMaterial);
            RenderTextureGameObject.GetComponent<MeshRenderer>().material = newMaterial;
        }
    }
}