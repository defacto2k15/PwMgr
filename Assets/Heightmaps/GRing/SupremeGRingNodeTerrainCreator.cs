using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Utils;

namespace Assets.Heightmaps.GRing
{
    public class SupremeGRingNodeTerrainCreator : INewGRingListenersCreator
    {
        private List<NewListenersCreatorWithLimitation> _creatorsList;

        public SupremeGRingNodeTerrainCreator(List<NewListenersCreatorWithLimitation> creatorsList)
        {
            _creatorsList = creatorsList;
        }

        public IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod lod)
        {
            var creatorsFilteredList = _creatorsList
                .Where(c => c.PositionLimiter == null || c.PositionLimiter.IsAccepted(node))
                .Where(c => c.MaximumLod.ScalarValue >= lod.ScalarValue)
                .OrderBy(c => c.MaximumLod.ScalarValue).Select(c => c.Creator).ToList();

            INewGRingListenersCreator creator = null;
            if (creatorsFilteredList.Any())
            {
                creator = creatorsFilteredList.First();
            }
            else
            {
                creator =
                    _creatorsList.Where(c => c.IsFallthroughCreator).Select(c => c.Creator).First();
            }

            return creator.CreateNewListener(node, lod);
        }
    }
}