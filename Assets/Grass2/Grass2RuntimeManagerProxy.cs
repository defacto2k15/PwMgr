using System.Collections.Generic;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Utils.MT;

namespace Assets.Grass2
{
    public class Grass2RuntimeManagerProxy : BaseOtherThreadProxy, IVegetationSubjectInstancingContainerChangeListener
    {
        private readonly Grass2RuntimeManager _grass2RuntimeManager;

        public Grass2RuntimeManagerProxy(Grass2RuntimeManager grass2RuntimeManager)
            : base("Grass2RuntimeManagerProxyThread", false)
        {
            _grass2RuntimeManager = grass2RuntimeManager;
        }

        public void AddInstancingOrder(
            VegetationDetailLevel level,
            List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            PostChainedAction(
                () => _grass2RuntimeManager.AddInstancingOrderAsync(level, gainedEntities, lostEntities));
        }
    }
}