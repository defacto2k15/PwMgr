using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.PostProcessing
{
    public class NPRPostProcessingDirectorOC : MonoBehaviour
    {
        public Material PostprocessingMaterial;
        private RenderBuffer[] _renderBuffers;
        private RenderTexture[] _renderTextures;

        public void Start()
        {
            GetComponent<Camera>().depthTextureMode = DepthTextureMode.None;

            var stage1TexturesCount = 4;

            _renderBuffers = new RenderBuffer[stage1TexturesCount];
            _renderTextures = new RenderTexture[stage1TexturesCount];
            for (int i = 0; i < 4; i++)
            {
                int depthBits = 0;
                if (i == 0)
                {
                    depthBits = 24;
                }
                var newTexture = new RenderTexture(Screen.width, Screen.height,depthBits, RenderTextureFormat.ARGB32 );
                _renderTextures[i] = newTexture;

                newTexture.filterMode = FilterMode.Point;
                newTexture.Create();
                _renderBuffers[i] = newTexture.colorBuffer;
            }
            GetComponent<Camera>().SetTargetBuffers(_renderBuffers, _renderTextures[0].depthBuffer);

            for (int i = 0; i < 4; i++)
            {
                PostprocessingMaterial.SetTexture("_TexBuffer" + i, _renderTextures[i]);
            }
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, PostprocessingMaterial, 0);

            var oldRenderTarget = RenderTexture.active;
            for (int i = 0; i < 4; i++)
            {
                RenderTexture.active = _renderTextures[i];
                GL.Clear(true, true, Color.clear);
            }
            RenderTexture.active = oldRenderTarget;
        }

    }
}
