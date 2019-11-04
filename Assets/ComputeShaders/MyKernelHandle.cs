namespace Assets.ComputeShaders
{
    public class MyKernelHandle
    {
    }

    public class MyInstancedKernelHandle
    {
        private int _handleId;

        public MyInstancedKernelHandle(int handleId)
        {
            _handleId = handleId;
        }

        public int HandleId
        {
            get { return _handleId; }
        }
    }
}