using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.SpotUpdating;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public interface IDesignBodyPortrayalForger
    {
        RepresentationCombinationInstanceId Forge( DesignBodyLevel1DetailWithSpotModification level1DetailWithSpotModification);
        void Remove(RepresentationCombinationInstanceId instanceId);
        void Modify(RepresentationCombinationInstanceId combinationInstanceId,  DesignBodyLevel1DetailWithSpotModification level1DetailWithSpotModification);
    }
}
