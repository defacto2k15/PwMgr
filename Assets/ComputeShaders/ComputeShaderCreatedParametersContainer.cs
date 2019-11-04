using System.Collections.Generic;
using Assets.Utils;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class ComputeShaderCreatedParametersContainer
    {
        private Dictionary<MyComputeShaderTextureId, Texture> _textures =
            new Dictionary<MyComputeShaderTextureId, Texture>();

        private Dictionary<MyComputeBufferId, ComputeBuffer> _buffers =
            new Dictionary<MyComputeBufferId, ComputeBuffer>();

        public void AddTexture(MyComputeShaderTextureId id, Texture texture)
        {
            _textures[id] = texture;
        }

        public void AddBuffer(MyComputeBufferId id, ComputeBuffer buffer)
        {
            _buffers[id] = buffer;
        }

        public Texture RetriveTexture(MyComputeShaderTextureId id)
        {
            Preconditions.Assert(_textures.ContainsKey(id), "There is no texture of id");
            return _textures[id];
        }

        public ComputeBuffer RetriveBuffer(MyComputeBufferId id)
        {
            Preconditions.Assert(_buffers.ContainsKey(id), " There is no buffer of given id ");
            return _buffers[id];
        }

        public Dictionary<MyComputeShaderTextureId, Texture> Textures => _textures;

        public Dictionary<MyComputeBufferId, ComputeBuffer> Buffers => _buffers;
    }
}