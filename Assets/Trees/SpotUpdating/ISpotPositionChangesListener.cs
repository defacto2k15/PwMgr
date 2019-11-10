using System.Collections.Generic;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;

namespace Assets.Trees.SpotUpdating
{
    public interface ISpotPositionChangesListener
    {
        void SpotsWereChanged(Dictionary<SpotId,  DesignBodySpotModification > changedSpots);
        void SpotGroupsWereChanged(Dictionary<SpotId, List<DesignBodySpotModification>> changedSpots);
    }
}