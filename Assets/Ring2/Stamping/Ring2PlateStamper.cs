using System;
using System.Collections.Generic;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.Devising;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Ring2.Stamping
{
    public class Ring2PlateStamperConfiguration
    {
        public Dictionary<int, float> PlateStampPixelsPerUnit;
    }

    public class Ring2PlateStamper
    {
        private Ring2PlateStamperConfiguration _configuration;
        private MultistepTextureRenderer _multistepTextureRenderer;

        private Action<Ring2PlateStamp> _completeCallback;
        private bool _generatingFirstImage = false;
        private MultistepTextureRenderingInput _input;
        private Texture _colorTexture;

        public Ring2PlateStamper(Ring2PlateStamperConfiguration configuration,
            ComputeShaderContainerGameObject computeShaderContainer)
        {
            _configuration = configuration;
            _multistepTextureRenderer = new MultistepTextureRenderer(computeShaderContainer);
        }

        public bool CurrentlyRendering
        {
            get { return _input != null; }
        }

        public void StartGeneratingPlateStamp(Ring2PlateStampTemplate template, Action<Ring2PlateStamp> completionCallback)
        {
            _completeCallback = completionCallback;
            var plateCoords = template.PlateCoords;
            float pixelsPerUnit = 3;
            if (_configuration.PlateStampPixelsPerUnit.ContainsKey(template.StampLod))
            {
                pixelsPerUnit = _configuration.PlateStampPixelsPerUnit[template.StampLod];
            }

            Vector2 imageSize = new Vector2(
                (int) Mathf.Round(pixelsPerUnit * plateCoords.Width),
                (int) Mathf.Round(pixelsPerUnit * plateCoords.Height)
            );

            ConventionalTextureInfo outTextureInfo = new ConventionalTextureInfo(
                (int) imageSize.x,
                (int) imageSize.y,
                TextureFormat.ARGB32,
                true
            );
            var renderMaterial = CreateRenderMaterial(template);

            _input = new MultistepTextureRenderingInput()
            {
                MultistepCoordUniform = new MultistepRenderingCoordUniform(
                    new Vector4(
                        template.PlateCoords.X,
                        template.PlateCoords.Y,
                        template.PlateCoords.Width,
                        template.PlateCoords.Height
                    ), "_Coords"),
                OutTextureinfo = outTextureInfo,
                RenderTextureInfoFormat = RenderTextureFormat.ARGB32,
                RenderMaterial = renderMaterial,
                StepSize = new Vector2(400, 400), //todo
                CreateTexture2D = false
            };
            RenderColorTexture();
        }

        public void Update()
        {
            if (_multistepTextureRenderer.RenderingCompleted())
            {
                if (_generatingFirstImage)
                {
                    _colorTexture = _multistepTextureRenderer.RetriveRenderedTexture();
                    RenderNormalsTexture();
                }
                else
                {
                    var normalTexture = _multistepTextureRenderer.RetriveRenderedTexture();
                    var callback = _completeCallback;
                    var outStamp = new Ring2PlateStamp(
                        _colorTexture,
                        normalTexture,
                        new IntVector2(_colorTexture.width, _colorTexture.height));
                    ClearRenderingData();
                    callback(outStamp);
                }
            }
            else
            {
                _multistepTextureRenderer.Update();
            }
        }

        private void RenderColorTexture()
        {
            _generatingFirstImage = true;
            var renderMaterial = _input.RenderMaterial;
            renderMaterial.EnableKeyword("GENERATE_COLOR");
            renderMaterial.DisableKeyword("GENERATE_NORMAL");
            _multistepTextureRenderer.StartRendering(_input);
        }

        private void RenderNormalsTexture()
        {
            _generatingFirstImage = false;
            var renderMaterial = _input.RenderMaterial;
            renderMaterial.DisableKeyword("GENERATE_COLOR");
            renderMaterial.EnableKeyword("GENERATE_NORMAL");
            _multistepTextureRenderer.StartRendering(_input);
        }

        private void ClearRenderingData()
        {
            _generatingFirstImage = false;
            _input = null;
            _colorTexture = null;
            _completeCallback = null;
        }

        public Ring2PlateStamp GeneratePlateStamp(Ring2PlateStampTemplate template)
        {
            var plateCoords = template.PlateCoords;
            float pixelsPerUnit = 3;
            if (_configuration.PlateStampPixelsPerUnit.ContainsKey(template.StampLod))
            {
                pixelsPerUnit = _configuration.PlateStampPixelsPerUnit[template.StampLod];
            }

            Vector2 imageSize = new Vector2(
                (int) Mathf.Round(pixelsPerUnit * plateCoords.Width),
                (int) Mathf.Round(pixelsPerUnit * plateCoords.Height)
            );

            RenderTextureInfo renderTextureInfo = new RenderTextureInfo(
                (int) imageSize.x,
                (int) imageSize.y,
                RenderTextureFormat.ARGB32
            );

            ConventionalTextureInfo outTextureInfo = new ConventionalTextureInfo(
                (int) imageSize.x,
                (int) imageSize.y,
                TextureFormat.ARGB32,
                true
            );

            Material renderMaterial = CreateRenderMaterial(template);
            renderMaterial.EnableKeyword("GENERATE_COLOR");

            Texture colorsTexture = UltraTextureRenderer.CreateRenderTexture(renderMaterial, renderTextureInfo);
            colorsTexture.wrapMode = TextureWrapMode.Clamp;

            renderMaterial.DisableKeyword("GENERATE_COLOR");
            renderMaterial.EnableKeyword("GENERATE_NORMAL");
            Texture normalTexture = UltraTextureRenderer.CreateRenderTexture(renderMaterial, renderTextureInfo);
            normalTexture.wrapMode = TextureWrapMode.Clamp;

            return new Ring2PlateStamp(colorsTexture, normalTexture, imageSize.ToIntVector());
        }


        private Material CreateRenderMaterial(Ring2PlateStampTemplate template)
        {
            var renderMaterial = new Material(Shader.Find("Custom/ETerrain/Ring2Stamper"));
            foreach (var keyword in template.MaterialTemplate.KeywordSet.Keywords)
            {
                renderMaterial.EnableKeyword(keyword);
            }
            template.MaterialTemplate.PropertyBlock.FillMaterial(renderMaterial);

            renderMaterial.SetVector("_Coords",
                new Vector4(
                    template.PlateCoords.X,
                    template.PlateCoords.Y,
                    template.PlateCoords.Width,
                    template.PlateCoords.Height
                ));
            return renderMaterial;
        }
    }

    public class Ring2PlateStamp
    {
        private Texture _colorStamp;
        private Texture _normalStamp;
        public Ring2PlateStamp(Texture colorStamp, Texture normalStamp, IntVector2 resolution)
        {
            _colorStamp = colorStamp;
            _normalStamp = normalStamp;
            Resolution = resolution;
        }

        public Texture ColorStamp => _colorStamp;
        public Texture NormalStamp => _normalStamp;
        public IntVector2 Resolution;

        public void Destroy()
        {
            Preconditions.Assert(_colorStamp!=null, "Ayy is null");
            GameObject.Destroy(ColorStamp);
            GameObject.Destroy(NormalStamp);
            _colorStamp = null;
            _normalStamp = null;
        }
    }

    public class Ring2PlateStampTemplate
    {
        public MaterialTemplate MaterialTemplate;
        public MyRectangle PlateCoords;
        public readonly int StampLod;

        public Ring2PlateStampTemplate(MaterialTemplate materialTemplate, MyRectangle plateCoords, int stampLod = 0)
        {
            MaterialTemplate = materialTemplate;
            PlateCoords = plateCoords;
            StampLod = stampLod;
        }
    }
}