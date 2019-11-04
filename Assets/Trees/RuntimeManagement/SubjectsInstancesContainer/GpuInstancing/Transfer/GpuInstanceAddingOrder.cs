using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class GpuInstanceAddingOrder
    {
        public GpuInstanceId GpuInstanceId { get; private set; }
        public Matrix4x4 Matrix { get; private set; }
        public UniformsPack UniformsPack { get; private set; }

        public GpuInstanceAddingOrder(GpuInstanceId gpuInstanceId, Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            GpuInstanceId = gpuInstanceId;
            Matrix = matrix;
            UniformsPack = uniformsPack;
        }
    }
}