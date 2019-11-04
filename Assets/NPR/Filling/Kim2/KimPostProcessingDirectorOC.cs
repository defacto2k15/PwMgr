using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.Filling.Kim2
{
    public class KimPostProcessingDirectorOC : MonoBehaviour
    {
        private RenderBuffer[] _renderBuffers;
        private RenderTexture[] _renderTextures;

        public Material PostprocessingMaterial;

        public void Start()
        {
            GetComponent<Camera>().depthTextureMode = DepthTextureMode.None;

            var stage1TexturesCount = 4;

            _renderTextures = new RenderTexture[stage1TexturesCount];

            var shadingTexture =  new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32 );
            shadingTexture.filterMode = FilterMode.Point;
            shadingTexture.Create();
            _renderTextures[0] = shadingTexture;

            var  geoPositionTexture =  new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            geoPositionTexture.filterMode = FilterMode.Point;
            geoPositionTexture.Create();
            _renderTextures[1] = geoPositionTexture;

            var  normalsTexture =  new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            normalsTexture.filterMode = FilterMode.Point;
            normalsTexture.Create();
            _renderTextures[2] = normalsTexture;

            var  tangentsTexture =  new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            tangentsTexture.filterMode = FilterMode.Point;
            tangentsTexture.Create();
            _renderTextures[3] = tangentsTexture;

            _renderBuffers = _renderTextures.Select(c => c.colorBuffer).ToArray();

            GetComponent<Camera>().SetTargetBuffers(_renderBuffers, _renderTextures[0].depthBuffer);


            PostprocessingMaterial.SetTexture("_ShadingTex", shadingTexture);
            PostprocessingMaterial.SetTexture("_GeoPositionTex", geoPositionTexture);
            PostprocessingMaterial.SetTexture("_NormalsTex", normalsTexture);
            PostprocessingMaterial.SetTexture("_TangentsTex", tangentsTexture);
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, PostprocessingMaterial, 0);

            var oldRenderTarget = RenderTexture.active;
            for (int i = 0; i < _renderTextures.Length; i++)
            {
                RenderTexture.active = _renderTextures[i];
                GL.Clear(true, true, Color.clear);
            }
            RenderTexture.active = oldRenderTarget;
        }
    }
}
