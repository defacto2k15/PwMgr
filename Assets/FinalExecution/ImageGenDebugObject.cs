using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class ImageGenDebugObject : MonoBehaviour
    {
        public GameObject TargetGameObject;

        public void Start()
        {
            CreateRing2Objects();
        }

        private void CreateRing2Objects()
        {
            var material = new Material(Shader.Find("Custom/Terrain/Ring2"));
            var controlTex = new Texture2D(4, 4, TextureFormat.ARGB32, false, true);
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var intensity = y / 3f;
                    controlTex.SetPixel(x, y, new Color(intensity, intensity, intensity, 1));
                }
            }
            controlTex.Apply();
            material.SetTexture("_ControlTex", controlTex);
            controlTex.wrapMode = TextureWrapMode.Clamp;

            List<Color> colorPalette = new List<Color>();
            for (int i = 0; i < 16; i++)
            {
                colorPalette.Add(new Color(0.2f + (i % 4) / 4f * 0 / 8f, 0, 0, 1));
            }

            material.SetColorArray("_Palette", colorPalette);
            material.SetVector("_Dimensions", Vector4.one);

            var renderer = TargetGameObject.GetComponent<MeshRenderer>();
            renderer.material = material;
        }
    }
}