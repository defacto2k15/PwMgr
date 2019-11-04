using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class ComputeBufferRequestedOutParameters
    {
        private List<MyComputeShaderTextureId> _requestedTexturesIds;
        private List<MyComputeBufferId> _requestedBufferIds;

        private Dictionary<MyComputeShaderTextureId, Texture> _createdTextures =
            new Dictionary<MyComputeShaderTextureId, Texture>();

        private Dictionary<MyComputeBufferId, ComputeBuffer> _createdBuffers =
            new Dictionary<MyComputeBufferId, ComputeBuffer>();

        public ComputeBufferRequestedOutParameters(List<MyComputeShaderTextureId> requestedTexturesIds = null,
            List<MyComputeBufferId> requestedBufferIds = null)
        {
            if (requestedBufferIds == null)
            {
                requestedBufferIds = new List<MyComputeBufferId>();
            }
            if (requestedTexturesIds == null)
            {
                requestedTexturesIds = new List<MyComputeShaderTextureId>();
            }
            _requestedTexturesIds = requestedTexturesIds;
            _requestedBufferIds = requestedBufferIds;
        }

        public ComputeBufferRequestedOutParameters( List<MyComputeBufferId> requestedBufferIds )
        {
            _requestedTexturesIds = new List<MyComputeShaderTextureId>();
            _requestedBufferIds = requestedBufferIds;
        }

        public void AddTexture(MyComputeShaderTextureId id, Texture texture)
        {
            Preconditions.Assert(_requestedTexturesIds.Contains(id), "Passed id if not requested");
            _createdTextures.Add(id, texture);
        }

        public void AddBuffer(MyComputeBufferId id, ComputeBuffer buffer)
        {
            Preconditions.Assert(_requestedBufferIds.Contains(id), "Passed id if not requested");
            _createdBuffers.Add(id, buffer);
        }

        public Dictionary<MyComputeShaderTextureId, Texture> CreatedTextures => _createdTextures;

        public Dictionary<MyComputeBufferId, ComputeBuffer> CreatedBuffers => _createdBuffers;

        public Texture RetriveTexture(MyComputeShaderTextureId id)
        {
            Preconditions.Assert(_createdTextures.ContainsKey(id), "There is no texture of given id");
            return _createdTextures[id];
        }

        public bool IsRequestedTextureId(MyComputeShaderTextureId id)
        {
            return _requestedTexturesIds.Contains(id);
        }

        public ComputeBuffer RetriveBuffer(MyComputeBufferId id)
        {
            Preconditions.Assert(_createdBuffers.ContainsKey(id), "There is no buffer of given id");
            return _createdBuffers[id];
        }

        public bool IsRequestedBufferId(MyComputeBufferId id)
        {
            return _requestedBufferIds.Contains(id);
        }
    }
}