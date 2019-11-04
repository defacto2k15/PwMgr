using System.Collections.Generic;
using Assets.ComputeShaders.Templating;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class ComputeShaderParametersContainer
    {
        private Dictionary<MyComputeShaderTextureId, MyComputeShaderTextureTemplate> _computeShaderTextureTemplates =
            new Dictionary<MyComputeShaderTextureId, MyComputeShaderTextureTemplate>();

        private Dictionary<MyComputeShaderTextureId, Texture> _arleadyCreatedTextures =
            new Dictionary<MyComputeShaderTextureId, Texture>();

        private int _lastTextureId = 0;

        private Dictionary<MyComputeBufferId, MyComputeBufferTemplate> _computeBufferTemplates =
            new Dictionary<MyComputeBufferId, MyComputeBufferTemplate>();

        private Dictionary<MyComputeBufferId, ComputeBuffer> _arleadyCreatedComputeBufferTemplates =
            new Dictionary<MyComputeBufferId, ComputeBuffer>();

        private int _lastBufferId = 0;

        public MyComputeBufferId AddComputeBufferTemplate(MyComputeBufferTemplate template)
        {
            var bufferId = new MyComputeBufferId()
            {
                Id = _lastBufferId++
            };
            _computeBufferTemplates[bufferId] = template;
            return bufferId;
        }

        public MyComputeBufferId AddExistingComputeBuffer(ComputeBuffer buffer)
        {
            var bufferId = new MyComputeBufferId()
            {
                Id = _lastBufferId++
            };
            _arleadyCreatedComputeBufferTemplates[bufferId] = buffer;
            return bufferId;
        }

        public MyComputeShaderTextureId AddComputeShaderTextureTemplate(MyComputeShaderTextureTemplate template)
        {
            var textureId = new MyComputeShaderTextureId()
            {
                Id = _lastTextureId++
            };
            _computeShaderTextureTemplates[textureId] = template;
            return textureId;
        }

        public MyComputeShaderTextureId AddExistingComputeShaderTexture(Texture texture)
        {
            var textureId = new MyComputeShaderTextureId()
            {
                Id = _lastTextureId++
            };
            _arleadyCreatedTextures[textureId] = texture;
            return textureId;
        }

        public Dictionary<MyComputeShaderTextureId, MyComputeShaderTextureTemplate> ComputeShaderTextureTemplates =>
            _computeShaderTextureTemplates;

        public Dictionary<MyComputeBufferId, MyComputeBufferTemplate> ComputeBufferTemplates => _computeBufferTemplates;

        public Dictionary<MyComputeShaderTextureId, Texture> ArleadyCreatedTextures => _arleadyCreatedTextures;

        public Dictionary<MyComputeBufferId, ComputeBuffer> ArleadyCreatedComputeBufferTemplates => _arleadyCreatedComputeBufferTemplates;
    }
}