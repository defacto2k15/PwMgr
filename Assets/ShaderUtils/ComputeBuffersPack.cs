using System.Collections.Generic;
using Assets.Utils.ShaderBuffers;
using UnityEngine;

namespace Assets.ShaderUtils
{
    public class ComputeBuffersPack
    {
        private readonly Dictionary<string, ComputeBuffer> _buffers = new Dictionary<string, ComputeBuffer>();
        private BufferReloaderRootGO _bufferReloader;

        public ComputeBuffersPack()
        {
        }

        public ComputeBuffersPack(BufferReloaderRootGO bufferReloader)
        {
            _bufferReloader = bufferReloader;
        }

        public ComputeBuffersPack( Dictionary<string, ComputeBuffer> buffers )
        {
            _buffers = buffers;
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
                if (_bufferReloader != null)
                {
                    _bufferReloader.RegisterBufferToReload(material, pair.Key, pair.Value);
                }
            }
        }
    }
}