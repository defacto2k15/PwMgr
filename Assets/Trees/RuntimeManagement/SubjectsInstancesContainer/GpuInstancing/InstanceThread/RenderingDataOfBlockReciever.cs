using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread
{
    public class RenderingDataOfBlockReciever
    {
        private GpuInstanceRenderingDataElementDelta _dataElementDelta = new GpuInstanceRenderingDataElementDelta();

        public bool UpdatesWereSynchronized
        {
            get { return _dataElementDelta.IsEmpty; }
        }

        public void Clear()
        {
            _dataElementDelta.Clear();
        }

        public void SetBlockFloatArray(string key, float[] value)
        {
            _dataElementDelta.SetBlockFloatArray(key, value);
        }

        public void SetBlockVectorArray(string key, Vector4[] value)
        {
            _dataElementDelta.SetBlockVectorArray(key, value);
        }

        public void SetBlockTexture(string key, Texture value)
        {
            _dataElementDelta.SetBlockTexture(key, value);
        }

        public void RecieveMaticesArray(Matrix4x4[] maticesArray)
        {
            _dataElementDelta.SetMaticesArray(maticesArray);
        }

        public void RecieveUsedCellsCount(int usedCellsCount)
        {
            _dataElementDelta.SetUsedCellsCount(usedCellsCount);
        }

        public void MoveData(GpuInstanceRenderingDataElementDelta other)
        {
            _dataElementDelta.MoveDataAndClear(other);
        }
    }
}