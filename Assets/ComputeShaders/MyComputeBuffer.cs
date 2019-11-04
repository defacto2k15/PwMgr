using UnityEngine;

namespace Assets.ComputeShaders
{
    public class MyComputeBuffer
    {
        private readonly string _bufferName;
        private ComputeBuffer _buffer;

        public MyComputeBuffer(string bufferName, int length, int stride)
        {
            _bufferName = bufferName;
            _buffer = new ComputeBuffer(length, stride);
        }

        public MyComputeBuffer(string bufferName, ComputeBuffer buffer)
        {
            _bufferName = bufferName;
            _buffer = buffer;
        }

        public string BufferName
        {
            get { return _bufferName; }
        }

        public ComputeBuffer Buffer
        {
            get { return _buffer; }
        }

        public void SetData(float[,] array)
        {
            _buffer.SetData(array);
        }

        public void GetData(float[,] outArray)
        {
            _buffer.GetData(outArray);
        }
    }
}