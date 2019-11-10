using System;
using System.Collections.Generic;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using UnityEngine;

namespace Assets.Trees.SpotUpdating
{
    public class LambdaSpotPositionChangesListener : ISpotPositionChangesListener
    {
        private readonly Action<Dictionary<SpotId,  DesignBodySpotModification>> _lambda;
        private readonly Action<Dictionary<SpotId, List< DesignBodySpotModification>>> _groupsLambda;

        public LambdaSpotPositionChangesListener(
            Action<Dictionary<SpotId,  DesignBodySpotModification>> lambda,
            Action<Dictionary<SpotId, List< DesignBodySpotModification>>> groupsLambda = null
        )
        {
            _lambda = lambda;
            _groupsLambda = groupsLambda;
        }

        public void SpotsWereChanged(Dictionary<SpotId,  DesignBodySpotModification> changedSpots)
        {
            _lambda(changedSpots);
        }

        public void SpotGroupsWereChanged(Dictionary<SpotId, List< DesignBodySpotModification>> changedSpots)
        {
            _groupsLambda(changedSpots);
        }
    }
}