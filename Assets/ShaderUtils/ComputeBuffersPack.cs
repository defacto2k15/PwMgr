using System.Collections.Generic;
using UnityEngine;

namespace Assets.ShaderUtils
{
    public class ComputeBuffersPack
    {
        private readonly Dictionary<string, ComputeBuffer> _buffers = new Dictionary<string, ComputeBuffer>();

        public ComputeBuffersPack()
        {
        }

        public void SetBuffer(string name, ComputeBuffer buffer)
        {
            _buffers[name] = buffer;
        }

        public Dictionary<string, ComputeBuffer> Buffers => _buffers;

        public void SetBuffersToMaterial(Material material)
        {
            foreach (var pair in _buffers)
            {
                material.SetBuffer(pair.Key, pair.Value);
            }
        }
    }
}