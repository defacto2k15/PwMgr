using System.Collections.Generic;
using System.Linq;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using MathNet.Numerics;

namespace Assets.Trees.DesignBodyDetails.DetailProvider
{
    public class DesignBodyLevel2DetailContainerForCombination
    {
        public List<DesignBodyLevel2Detail> PerElementDetails;

        public DesignBodyLevel2DetailContainerForCombination(List<DesignBodyLevel2Detail> perElementDetails)
        {
            PerElementDetails = perElementDetails;
        }

        public DesignBodyLevel2Detail GetDetailFor(int index)
        {
            return PerElementDetails[index];
        }

        public DesignBodyLevel2DetailContainerForCombination MergeWithAdditionalDetails(DesignBodyLevel2Detail other)
        {
            return new DesignBodyLevel2DetailContainerForCombination(PerElementDetails.Select(c => c.MergeNewWith(other)).ToList());
        }
    }
}