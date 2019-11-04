using System;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread
{
    public class GpuInstancingUniformTemplate
    {
        private String _name;
        private GpuInstancingUniformType _type;

        public GpuInstancingUniformTemplate(string name, GpuInstancingUniformType type)
        {
            _name = name;
            _type = type;
        }

        public string Name
        {
            get { return _name; }
        }

        public GpuInstancingUniformType Type
        {
            get { return _type; }
        }
    }
}