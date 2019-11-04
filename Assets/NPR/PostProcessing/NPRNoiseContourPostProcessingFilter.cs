using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.PostProcessing
{
    public class NPRNoiseContourPostProcessingFilter : MonoBehaviour
    {
        public Material NoiseContourMaterial;
        private RenderTexture _bufferTexture;
        private Camera _camera;

        public void Start() // NoiseContour
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.DepthNormals;
            _bufferTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            _bufferTexture.Create();
            NoiseContourMaterial.SetTexture("_BufferTex", _bufferTexture);
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest) // NoiseContour
        {
            var p = GL.GetGPUProjectionMatrix(_camera.projectionMatrix,
                false); // Unity flips its 'Y' vector depending on if its in VR, Editor view or game view etc... (facepalm)
            p[2, 3] = p[3, 2] = 0.0f;
            p[3, 3] = 1.0f;
            var clipToWorld = Matrix4x4.Inverse(p * _camera.worldToCameraMatrix) *
                              Matrix4x4.TRS(new Vector3(0, 0, -p[2, 2]), Quaternion.identity, Vector3.one);
            NoiseContourMaterial.SetMatrix("_ClipToWorld", clipToWorld);

            Graphics.Blit(src, _bufferTexture, NoiseContourMaterial,0);
            Graphics.Blit(src, dest, NoiseContourMaterial,1);
        }
    }
}
