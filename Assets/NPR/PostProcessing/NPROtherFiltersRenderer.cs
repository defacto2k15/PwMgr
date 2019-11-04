using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.PostProcessing
{
    public class NPROtherFiltersRenderer : MonoBehaviour
    {
        public Material FilterMaterial;
        private Camera _camera;
        private RenderTexture _bufferTex;
        public int IterationsCount = 1;

        public void Start() // NoiseContour
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.DepthNormals;
            _bufferTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            _bufferTex.Create();
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest) // NoiseContour
        {
            var p = GL.GetGPUProjectionMatrix(_camera.projectionMatrix,
                false); // Unity flips its 'Y' vector depending on if its in VR, Editor view or game view etc... (facepalm)
            p[2, 3] = p[3, 2] = 0.0f;
            p[3, 3] = 1.0f;
            var clipToWorld = Matrix4x4.Inverse(p * _camera.worldToCameraMatrix) *
                              Matrix4x4.TRS(new Vector3(0, 0, -p[2, 2]), Quaternion.identity, Vector3.one);
            FilterMaterial.SetMatrix("_ClipToWorld", clipToWorld);

            if (IterationsCount <= 1)
            {
                Graphics.Blit(src, dest, FilterMaterial);
            }
            else
            {
                RenderTexture mySrc = src;
                RenderTexture myDst = _bufferTex;
                for (int i = 0; i < IterationsCount - 1; i++)
                {
                    Graphics.Blit(mySrc, myDst, FilterMaterial);
                    var temp = mySrc;
                    mySrc = myDst;
                    myDst = temp;
                }
                Graphics.Blit(mySrc, dest, FilterMaterial);
            }
        }
    }
}
