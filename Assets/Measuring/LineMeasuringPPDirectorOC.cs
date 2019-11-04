using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Measuring
{
    public class LineMeasuringPpDirectorOc : MonoBehaviour
    {
        private RenderTexture _hatchMainRenderTexture;
        private RenderTexture _idRenderTexture;
        private RenderTexture _worldPos1RenderTexture;
        private RenderTexture _worldPos2RenderTexture;

        private Texture2D _hatchMainTex2D;
        private Texture2D _hatchIdTex2D;
        private Texture2D _worldPos1Tex2D;
        private Texture2D _worldPos2Tex2D;

        private Queue<Action<MeasurementScreenshotsSet>> _screenshotCallbacks = new Queue<Action<MeasurementScreenshotsSet>>();

        public void Start()
        {
            _hatchMainRenderTexture = new RenderTexture(Screen.width, Screen.height,24, RenderTextureFormat.ARGB32 );
            _hatchMainTex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };

            _idRenderTexture = new RenderTexture(Screen.width, Screen.height,0, RenderTextureFormat.ARGB32 );
            _hatchIdTex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };

            _worldPos1RenderTexture = new RenderTexture(Screen.width, Screen.height,0, RenderTextureFormat.ARGB32 );
            _worldPos1Tex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };

            _worldPos2RenderTexture = new RenderTexture(Screen.width, Screen.height,0, RenderTextureFormat.ARGB32 );
            _worldPos2Tex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };

            var renderBuffers = new RenderBuffer[4]
            {
                _hatchMainRenderTexture.colorBuffer, _idRenderTexture.colorBuffer, _worldPos1RenderTexture.colorBuffer, _worldPos2RenderTexture.colorBuffer,
            };

            GetComponent<Camera>().SetTargetBuffers(renderBuffers, _hatchMainRenderTexture.depthBuffer);
        }

        public RenderTexture HatchMainRenderTexture => _hatchMainRenderTexture;

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest);

            if (_screenshotCallbacks.Any())
            {
                FulfillScreenshotRequirements();
            }

            var oldRenderTarget = RenderTexture.active;

            RenderTexture.active = _hatchMainRenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = _idRenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = _worldPos1RenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = _worldPos2RenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = oldRenderTarget;
        }

        public void RequireScreenshotsSet(Action<MeasurementScreenshotsSet> callback)
        {
            _screenshotCallbacks.Enqueue(callback);
        }

        public void FulfillScreenshotRequirements()
        {
            var set = GenerateScreenshotsSet();
            foreach (var c in _screenshotCallbacks)
            {
                c(set);
            }
            _screenshotCallbacks.Clear();
        }

        private MeasurementScreenshotsSet GenerateScreenshotsSet()
        {
            UltraTextureRenderer.RenderIntoExistingTexture2D(_hatchMainRenderTexture, this._hatchMainTex2D);
            UltraTextureRenderer.RenderIntoExistingTexture2D(_idRenderTexture, this._hatchIdTex2D);
            UltraTextureRenderer.RenderIntoExistingTexture2D(_worldPos1RenderTexture, this._worldPos1Tex2D);
            UltraTextureRenderer.RenderIntoExistingTexture2D(_worldPos2RenderTexture, this._worldPos2Tex2D);

            return new MeasurementScreenshotsSet()
            {
                HatchIdTexture = LocalTexture.FromTexture2D(_hatchIdTex2D),
                HatchMainTexture = LocalTexture.FromTexture2D(_hatchMainTex2D),
                WorldPosition1Texture = LocalTexture.FromTexture2D(_worldPos1Tex2D),
                WorldPosition2Texture = LocalTexture.FromTexture2D(_worldPos2Tex2D),
            };
        }
    }
}
