using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Measuring
{
    public class LineMeasuringPpModule
    {
        private Camera _camera;
        private RenderTexture _artisticMainRenderTexture;
        private RenderTexture _hatchMainRenderTexture;
        private RenderTexture _idRenderTexture;
        private RenderTexture _worldPos1RenderTexture;
        private RenderTexture _worldPos2RenderTexture;

        private Texture2D _hatchMainTex2D;
        private Texture2D _hatchIdTex2D;
        private Texture2D _worldPos1Tex2D;
        private Texture2D _worldPos2Tex2D;

        public void Initialize(Camera camera)
        {
            _camera = camera;
            _artisticMainRenderTexture = new RenderTexture(Screen.width, Screen.height,24, RenderTextureFormat.ARGB32 );
            _artisticMainRenderTexture.filterMode = FilterMode.Point;

            _hatchMainRenderTexture = new RenderTexture(Screen.width, Screen.height,24, RenderTextureFormat.ARGB32 );
            _hatchMainRenderTexture.filterMode = FilterMode.Point;
            _hatchMainTex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };

            _idRenderTexture = new RenderTexture(Screen.width, Screen.height,0, RenderTextureFormat.ARGB32 );
            _idRenderTexture.filterMode = FilterMode.Point;
            _hatchIdTex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };

            _worldPos1RenderTexture = new RenderTexture(Screen.width, Screen.height,0, RenderTextureFormat.ARGB32 );
            _worldPos1RenderTexture.filterMode = FilterMode.Point;
            _worldPos1Tex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };

            _worldPos2RenderTexture = new RenderTexture(Screen.width, Screen.height,0, RenderTextureFormat.ARGB32 );
            _worldPos2RenderTexture.filterMode = FilterMode.Point;
            _worldPos2Tex2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
        }

        public void SetTargetBuffers()
        {
            var renderBuffers = new RenderBuffer[5]
            {
                _artisticMainRenderTexture.colorBuffer, _hatchMainRenderTexture.colorBuffer, _idRenderTexture.colorBuffer, _worldPos1RenderTexture.colorBuffer, _worldPos2RenderTexture.colorBuffer,
            };
            _camera.SetTargetBuffers(renderBuffers,  _artisticMainRenderTexture.depthBuffer);
        }

        public MeasurementRenderTargetsSet RenderTargets => new MeasurementRenderTargetsSet()
        {
            ArtisticMainTexture = _artisticMainRenderTexture,
            HatchMainTexture = _hatchMainRenderTexture,
            HatchIdTexture = _idRenderTexture,
            WorldPosition1Texture = _worldPos1RenderTexture,
            WorldPosition2Texture = _worldPos2RenderTexture
        };

        public MeasurementScreenshotsSet OnRenderImageGenerateScreenshots()
        {
            var outSet = GenerateScreenshotsSet();
            var oldRenderTarget = RenderTexture.active;

            RenderTexture.active = _artisticMainRenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = _hatchMainRenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = _idRenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = _worldPos1RenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = _worldPos2RenderTexture;
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = oldRenderTarget;

            return outSet;
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
                HatchMainTexture2D = _hatchMainTex2D
            };
        }

        public void MyDestroy()
        {
            var allTextures = new List<Texture>()
            {
                _artisticMainRenderTexture,
                _hatchMainRenderTexture,
                _idRenderTexture,
                _worldPos1RenderTexture,
                _worldPos2RenderTexture,

                _hatchMainTex2D,
                _hatchIdTex2D,
                _worldPos1Tex2D,
                _worldPos2Tex2D,
            };
            allTextures.ForEach(GameObject.Destroy);
        }
    }
}