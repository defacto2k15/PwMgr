using System;
using Assets.Grass;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread
{
    public class GpuInstancingBlock
    {
        private Matrix4x4[] _maticesArray = new Matrix4x4[MyConstants.MaxInstancesPerPack];
        private GpuInstancingUniformsArray _uniformsArray;
        private int _usedCellsCount;
        private bool _shouldUpdate = false;
        private Int32 _id;

        public GpuInstancingBlock(GpuInstancingUniformsArrayTemplate uniformsArrayTemplate, Int32 id)
        {
            this._uniformsArray = new GpuInstancingUniformsArray(uniformsArrayTemplate);
            _id = id;
        }

        public bool HasFreeSpace()
        {
            return _usedCellsCount < MyConstants.MaxInstancesPerPack;
        }

        public int UsedCellsCount
        {
            get { return _usedCellsCount; }
        }

        public Matrix4x4[] MaticesArray
        {
            get { return _maticesArray; }
        }

        public int Id
        {
            get { return _id; }
        }

        public Int32 AddInstance(Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            Int32 index = _usedCellsCount;
            _usedCellsCount = _usedCellsCount + 1;
            FillCell(index, matrix, uniformsPack);
            return index;
        }

        public void ModifyInstance(int index, Matrix4x4? matrix, UniformsPack uniformsPack)
        {
            Matrix4x4 nonNullMatrix;
            if (matrix.HasValue)
            {
                nonNullMatrix = matrix.Value;
            }
            else
            {
                nonNullMatrix = _maticesArray[index];
            }

            UniformsPack nonNullUniformsPack;
            if (uniformsPack != null)
            {
                nonNullUniformsPack = uniformsPack;
            }
            else
            {
                nonNullUniformsPack=new UniformsPack();
            }

            FillCell(index,nonNullMatrix,nonNullUniformsPack);
        }

        public void Update(RenderingDataOfBlockReciever reciever)
        {
            if (_shouldUpdate)
            {
                _uniformsArray.FillMaterialPropertyBlock(reciever);
                reciever.RecieveMaticesArray(_maticesArray);
                reciever.RecieveUsedCellsCount(_usedCellsCount);
                _shouldUpdate = false;
            }
        }

        public void MoveAndRemove(int removedCellId, int elementToSwap)
        {
                _maticesArray[removedCellId] = _maticesArray[elementToSwap];
            _uniformsArray.Move(elementToSwap, removedCellId);
            _usedCellsCount = _usedCellsCount - 1;
            _shouldUpdate = true;
        }

        public void FillCell(int index, Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            _maticesArray[index] = matrix;
            _uniformsArray.SetUniform(index, uniformsPack);
            _shouldUpdate = true;
        }

        public void RemoveLast()
        {
            Preconditions.Assert(_usedCellsCount == 1, "There should be one used cell, but is " + _usedCellsCount);
            _usedCellsCount = _usedCellsCount - 1;
            _shouldUpdate = true;
        }

        public void MoveToOtherBlock(int thisBlockCellIndex, GpuInstancingBlock otherBlock)
        {
            var otherBlockCellIndex = otherBlock.UsedCellsCount;
            otherBlock._maticesArray[otherBlockCellIndex] = _maticesArray[thisBlockCellIndex];
            _uniformsArray.MoveToOtherArray(thisBlockCellIndex, otherBlock._uniformsArray, otherBlockCellIndex);
            otherBlock._usedCellsCount = otherBlock._usedCellsCount + 1;
            otherBlock._shouldUpdate = true;
        }
    }
}