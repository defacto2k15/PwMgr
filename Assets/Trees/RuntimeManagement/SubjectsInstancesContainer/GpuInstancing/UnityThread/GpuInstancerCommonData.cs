using Assets.ShaderUtils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread
{
    public class GpuInstancerCommonData
    {
        private Mesh _mesh;
        private Material _material;
        private int _instancesCount = 0;
        private ShadowCastingMode _castShadows = ShadowCastingMode.On;
        private int _submeshIndex = 0;
        private UniformsPack _uniformsPack;

        public GpuInstancerCommonData(Mesh mesh, Material material, UniformsPack uniformsPack)
        {
            _mesh = mesh;
            _material = material;
            _uniformsPack = uniformsPack;
        }

        public GpuInstancerCommonData()
        {
        }

        public Mesh Mesh
        {
            set { _mesh = value; }
            get { return _mesh; }
        }

        public Material Material
        {
            set { _material = value; }
            get { return _material; }
        }

        public int InstancesCount
        {
            get { return _instancesCount; }
        }

        public ShadowCastingMode CastShadows
        {
            set { _castShadows = value; }
            get { return _castShadows; }
        }

        public int SubmeshIndex
        {
            set { _submeshIndex = value; }
            get { return _submeshIndex; }
        }

        public UniformsPack UniformsPack
        {
            set { _uniformsPack = value; }
            get { return _uniformsPack; }
        }
    }
}