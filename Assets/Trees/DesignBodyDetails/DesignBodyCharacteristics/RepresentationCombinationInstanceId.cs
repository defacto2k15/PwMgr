using System.Collections.Generic;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;

namespace Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics
{
    public struct RepresentationCombinationInstanceId
    {
        public List<GpuBucketedInstanceId> InstanceIds;

        public RepresentationCombinationInstanceId(List<GpuBucketedInstanceId> instanceIds)
        {
            InstanceIds = instanceIds;
        }
    }
}