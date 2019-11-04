using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.CodePostProcessing
{
    public class SkyRenderer : MonoBehaviour
    {
        private Material _material;
        private Camera _camera;

        [Range(0, 1f)] public float Param1 = 0;
        [Range(0, 1f)] public float Param2 = 0;

        public Vector3 WindStrength = new Vector3(0.02f, 0.02f, 0.01f);

        private Vector3 CloudPositionOffset = new Vector3(123.221f, 95.99f, 1.33f);

        public void Start()
        {
            _material = new Material(Shader.Find("Custom/PostProcessing"));
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.Depth;
        }

        public void Update()
        {
            CloudPositionOffset += Time.deltaTime * WindStrength;
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            var p = GL.GetGPUProjectionMatrix(_camera.projectionMatrix,
                false); // Unity flips its 'Y' vector depending on if its in VR, Editor view or game view etc... (facepalm)
            p[2, 3] = p[3, 2] = 0.0f;
            p[3, 3] = 1.0f;
            var clipToWorld = Matrix4x4.Inverse(p * _camera.worldToCameraMatrix) *
                              Matrix4x4.TRS(new Vector3(0, 0, -p[2, 2]), Quaternion.identity, Vector3.one);
            _material.SetMatrix("_ClipToWorld", clipToWorld);

            _material.SetFloat("_Param1", Param1);
            _material.SetFloat("_Param2", Param2);
            _material.SetVector("_CloudPositionOffset", CloudPositionOffset);


            Graphics.Blit(src, dest, _material);
        }
    }
}