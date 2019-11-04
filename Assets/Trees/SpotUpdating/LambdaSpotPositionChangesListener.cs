using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Trees.SpotUpdating
{
    public class LambdaSpotPositionChangesListener : ISpotPositionChangesListener
    {
        private readonly Action<Dictionary<SpotId, SpotData>> _lambda;
        private readonly Action<Dictionary<SpotId, List<SpotData>>> _groupsLambda;

        public LambdaSpotPositionChangesListener(
            Action<Dictionary<SpotId, SpotData>> lambda,
            Action<Dictionary<SpotId, List<SpotData>>> groupsLambda = null
        )
        {
            _lambda = lambda;
            _groupsLambda = groupsLambda;
        }

        public void SpotsWereChanged(Dictionary<SpotId, SpotData> changedSpots)
        {
            _lambda(changedSpots);
        }

        public void SpotGroupsWereChanged(Dictionary<SpotId, List<SpotData>> changedSpots)
        {
            _groupsLambda(changedSpots);
        }
    }
}