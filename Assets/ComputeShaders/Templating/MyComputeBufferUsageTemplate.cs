using System.Collections.Generic;

namespace Assets.ComputeShaders.Templating
{
    public class MyComputeBufferUsageTemplate
    {
        private readonly string _bufferName;
        private readonly MyComputeBufferId _bufferId;
        private readonly List<MyKernelHandle> _handles;

        public MyComputeBufferUsageTemplate(string bufferName, MyComputeBufferId bufferId, List<MyKernelHandle> handles)
        {
            _bufferName = bufferName;
            _bufferId = bufferId;
            _handles = handles;
        }

        public string BufferName => _bufferName;

        public MyComputeBufferId BufferId => _bufferId;

        public List<MyKernelHandle> Handles => _handles;
    }
}