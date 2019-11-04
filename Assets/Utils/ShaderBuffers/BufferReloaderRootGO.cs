using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils.ShaderBuffers
{
    public class BufferReloaderRootGO : MonoBehaviour
    {
        private Dictionary<Material, List<BufferToReloadInfo>> _buffersToReload = new Dictionary<Material, List<BufferToReloadInfo>>();

        public void RegisterBufferToReload(Material material, string bufferName, ComputeBuffer buffer)
        {
            if (!_buffersToReload.ContainsKey(material))
            {
                _buffersToReload[material] = new List<BufferToReloadInfo>();
            }
            _buffersToReload[material].Add( new BufferToReloadInfo()
            {
                Buffer = buffer,
                BufferName = bufferName
            });
        }

        public void UpdateBuffers()
        {
            foreach (var pair in _buffersToReload)
            {
                foreach (var bufferToReloadInfo in pair.Value)
                {
                    pair.Key.SetBuffer(bufferToReloadInfo.BufferName, bufferToReloadInfo.Buffer);
                }
            }
        }
    }

    public class BufferToReloadInfo
    {
        public string BufferName;
        public ComputeBuffer Buffer;
    }
}
