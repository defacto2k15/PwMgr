using System.Collections.Generic;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread
{
    public class GpuInstancingUniformsArrayTemplate
    {
        public List<GpuInstancingUniformTemplate> UniformTemplates { get; private set; }

        public GpuInstancingUniformsArrayTemplate(List<GpuInstancingUniformTemplate> uniformTemplates)
        {
            this.UniformTemplates = uniformTemplates;
        }
    }
}