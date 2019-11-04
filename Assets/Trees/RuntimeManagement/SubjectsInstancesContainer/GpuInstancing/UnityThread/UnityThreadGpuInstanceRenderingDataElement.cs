using System;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread
{
    public class UnityThreadGpuInstanceRenderingDataElement
    {
        private Int32 _blockId;

        public MaterialPropertyBlock Block { get; set; }
        public Matrix4x4[] MaticesArray { get; set; }
        public int UsedCellsCount { get; set; }

        public UnityThreadGpuInstanceRenderingDataElement(int blockId, UniformsPack baseUniformsPack)
        {
            Block = new MaterialPropertyBlock();
            MaticesArray = null;
            UsedCellsCount = 0;
            _blockId = blockId;

            foreach (var uniform in baseUniformsPack.FloatUniforms)
            {
                Block.SetFloat(uniform.Key, uniform.Value.Get());
            }

            foreach (var uniform in baseUniformsPack.Vector4Uniforms)
            {
                Block.SetVector(uniform.Key, uniform.Value.Get());
            }

            foreach (var uniform in baseUniformsPack.Textures)
            {
                Block.SetTexture(uniform.Key, uniform.Value);
            }
        }

        public int BlockId
        {
            get { return _blockId; }
        }

        public void ApplyDelta(GpuInstanceRenderingDataElementDelta delta)
        {
            foreach (var pair in delta.MyBlock.FloatUniforms)
            {
                Block.SetFloatArray(pair.Key, pair.Value);
            }
            foreach (var pair in delta.MyBlock.Vector4Uniforms)
            {
                Block.SetVectorArray(pair.Key, pair.Value);
            }
            foreach (var pair in delta.MyBlock.TextureUniforms)
            {
                Block.SetTexture(pair.Key, pair.Value);
            }
            if (delta.UsedCellsCount != -1)
            {
                UsedCellsCount = delta.UsedCellsCount;
            }
            if (delta.MaticesArray != null)
            {
                MaticesArray = delta.MaticesArray;
            }
        }
    }
}