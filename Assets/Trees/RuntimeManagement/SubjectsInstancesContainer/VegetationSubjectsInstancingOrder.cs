using System.Collections.Generic;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer
{
    public class VegetationSubjectsInstancingOrder
    {
        private VegetationDetailLevel _level;
        private Queue<VegetationSubjectEntity> _creationList;
        private Queue<VegetationSubjectEntity> _removalList;

        public VegetationSubjectsInstancingOrder(Queue<VegetationSubjectEntity> creationList,
            Queue<VegetationSubjectEntity> removalList, VegetationDetailLevel level)
        {
            _creationList = creationList;
            _removalList = removalList;
            this._level = level;
        }

        public Queue<VegetationSubjectEntity> CreationList
        {
            get { return _creationList; }
        }

        public Queue<VegetationSubjectEntity> RemovalList
        {
            get { return _removalList; }
        }

        public VegetationDetailLevel Level
        {
            get { return _level; }
        }
    }
}