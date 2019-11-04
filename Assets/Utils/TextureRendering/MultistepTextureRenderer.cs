using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using UnityEngine;

namespace Assets.Utils.TextureRendering
{
    public class MultistepTextureRenderer
    {
        private MultistepTextureRenderingInput _input;
        private MultistepTextureRenderingState _state = null;
        private ComputeShaderContainerGameObject _computeShaderContainer;

        public MultistepTextureRenderer(ComputeShaderContainerGameObject computeShaderContainer)
        {
            _computeShaderContainer = computeShaderContainer;
        }

        public void StartRendering(MultistepTextureRenderingInput input)
        {
            _input = input;
            _input.ComputeShadersContainer = _computeShaderContainer;
            _state = UltraTextureRenderer.MultistepRenderTexture(_input, _state);
        }

        public void Update()
        {
            Preconditions.Assert(_state == null || !_state.RenderingCompleted, $"Rendering is arleady completed {_state==null}");
            _state = UltraTextureRenderer.MultistepRenderTexture(_input, _state);
        }

        public bool RenderingCompleted()
        {
            return _state.RenderingCompleted;
        }

        public Texture RetriveRenderedTexture()
        {
            Preconditions.Assert(_state.RenderingCompleted, "Rendering is not completed");
            var toReturn = _state.OutTexture;
            _input = null;
            _state = null;
            return toReturn;
        }

        public bool IsActive
        {
            get
            {
                return _state != null;
            }
        }
    }
}