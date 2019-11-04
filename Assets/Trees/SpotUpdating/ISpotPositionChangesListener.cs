using System.Collections.Generic;

namespace Assets.Trees.SpotUpdating
{
    public interface ISpotPositionChangesListener
    {
        void SpotsWereChanged(Dictionary<SpotId, SpotData> changedSpots);
        void SpotGroupsWereChanged(Dictionary<SpotId, List<SpotData>> changedSpots);
    }
}