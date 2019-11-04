using System.Collections.Generic;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer
{
    public interface IVegetationSubjectInstancingContainerChangeListener
    {
        void AddInstancingOrder(VegetationDetailLevel level, List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities);
    }
}