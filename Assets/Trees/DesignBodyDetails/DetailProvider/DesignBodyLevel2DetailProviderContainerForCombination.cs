using System.Collections.Generic;

namespace Assets.Trees.DesignBodyDetails.DetailProvider
{
    public class DesignBodyLevel2DetailProviderContainerForCombination
    {
        public AbstractDesignBodyLevel2DetailProvider BaseDetailProvider;
        public List<AbstractDesignBodyLevel2DetailProvider> PerCombinationElementDetailProviders;

        public DesignBodyLevel2DetailProviderContainerForCombination(
            AbstractDesignBodyLevel2DetailProvider baseDetailProvider,
            List<AbstractDesignBodyLevel2DetailProvider> perCombinationElementDetailProviders)
        {
            BaseDetailProvider = baseDetailProvider;
            PerCombinationElementDetailProviders = perCombinationElementDetailProviders;
        }
    }
}