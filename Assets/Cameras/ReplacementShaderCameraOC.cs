using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.Cameras
{
    public class ReplacementShaderCameraOC : MonoBehaviour
    {
        public Camera SourceCamera;
        private Camera _ourCamera;
        private RenderTexture _targetTexture;

        public void Start()
        {
            if (_ourCamera == null)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            _ourCamera = GetComponent<Camera>();
            _targetTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            _targetTexture.Create();
        }

        public void MySetReplacementShader(Shader shader, string replacementTag)
        {
            if (_ourCamera == null)
            {
                Initialize();
            }
            _ourCamera.SetReplacementShader(shader, replacementTag);
        }

        public void RenderToTarget()
        {
            if (_ourCamera == null)
            {
                Initialize();
            }

            CopySettingsFromOtherCamera();

            _ourCamera.enabled = true;
            _ourCamera.targetTexture = _targetTexture;
            _ourCamera.Render();
            _ourCamera.enabled = false;
        }

        private void CopySettingsFromOtherCamera()
        {
            _ourCamera.transform.position = SourceCamera.transform.position;
            _ourCamera.transform.rotation = SourceCamera.transform.rotation;
            _ourCamera.fieldOfView = SourceCamera.fieldOfView;
        }

        public RenderTexture TargetTexture => _targetTexture;
    }
}
