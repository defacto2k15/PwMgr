using System;
using System.Linq;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class GpuInstanceRenderingDataElementDelta
    {
        private MyMaterialPropertyBlockUniformsContainer _myBlock = new MyMaterialPropertyBlockUniformsContainer();
        private Matrix4x4[] _maticesArray;
        private int _usedCellsCount = -1;

        public bool IsEmpty
        {
            get { return !(_myBlock.AnythingThere || _maticesArray != null || _usedCellsCount != -1); }
        }

        public void Clear()
        {
            _usedCellsCount = -1;
            _maticesArray = null;
            _myBlock.Clear();
        }

        public void SetBlockFloatArray(string key, float[] value)
        {
            _myBlock.SetCopyOfFloatArray(key, value);
        }

        public void SetBlockVectorArray(string key, Vector4[] value)
        {
            _myBlock.SetCopyOfVectorArray(key, value);
        }

        public void SetBlockTexture(string key, Texture value)
        {
            _myBlock.SetTexture(key, value);
        }

        public void SetMaticesArray(Matrix4x4[] maticesArray)
        {
            var newArray = new Matrix4x4[maticesArray.Length];
            Array.Copy(maticesArray, newArray, maticesArray.Count());
            _maticesArray = newArray;
        }


        public void MoveMaticesArray(Matrix4x4[] maticesArray)
        {
            _maticesArray = maticesArray;
        }

        public void SetUsedCellsCount(int usedCellsCount)
        {
            _usedCellsCount = usedCellsCount;
        }

        public void MoveDataAndClear(GpuInstanceRenderingDataElementDelta other)
        {
            _myBlock.MoveData(other._myBlock);
            if (_usedCellsCount != -1)
            {
                other.SetUsedCellsCount(_usedCellsCount);
            }
            if (_maticesArray != null)
            {
                other.MoveMaticesArray(_maticesArray);
            }
            Clear();
        }

        public MyMaterialPropertyBlockUniformsContainer MyBlock
        {
            get { return _myBlock; }
        }

        public Matrix4x4[] MaticesArray
        {
            get { return _maticesArray; }
        }

        public int UsedCellsCount
        {
            get { return _usedCellsCount; }
        }
    }
}