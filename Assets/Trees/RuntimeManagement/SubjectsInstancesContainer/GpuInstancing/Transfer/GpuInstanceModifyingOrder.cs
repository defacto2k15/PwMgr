using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class GpuInstanceModifyingOrder
    {
        public GpuInstanceId InstanceId { get; private set; }
        public Matrix4x4 Matrix { get; private set; }
        public UniformsPack UniformsPack { get; private set; }

        public GpuInstanceModifyingOrder(GpuInstanceId instanceId, Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            InstanceId = instanceId;
            Matrix = matrix;
            UniformsPack = uniformsPack;
        }
    }
}