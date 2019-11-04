using System;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing
{
    public class GpuInstanceId
    {
        private Int32 _goodBlockIndex;
        private Int32 _cellId;

        public GpuInstanceId(Int32 goodBlockIndex, Int32 cellId)
        {
            _goodBlockIndex = goodBlockIndex;
            _cellId = cellId;
        }

        public int GoodBlockIndex
        {
            get { return _goodBlockIndex; }
        }

        public int CellId
        {
            get { return _cellId; }
        }

        public static GpuInstanceId CreateEmpty()
        {
            return new GpuInstanceId(-1, -1);
        }

        public void FillFrom(GpuInstanceId other)
        {
            _goodBlockIndex = other._goodBlockIndex;
            _cellId = other._cellId;
        }

        public void FillFrom(int blockIndex, int cellId)
        {
            _goodBlockIndex = blockIndex;
            _cellId = cellId;
        }

        public bool IsEmpty => _goodBlockIndex == -1 && _cellId == -1;
    }
}