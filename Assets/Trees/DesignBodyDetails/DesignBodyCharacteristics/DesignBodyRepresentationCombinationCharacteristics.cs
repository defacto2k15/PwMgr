using System.Collections.Generic;
using Assets.Trees.DesignBodyDetails.DetailProvider;

namespace Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics
{
    public class DesignBodyRepresentationCombinationCharacteristics
    {
        public List<AbstractDesignBodyLevel2DetailProvider> PerCombinationElementDetailProviders;
        public AbstractDesignBodyLevel2DetailProvider BaseDetailProvider;
        public ISpecificDesignBodyLevel2DetailsGenerator SpecificGenerator;

        public DesignBodyRepresentationCombinationCharacteristics(
            AbstractDesignBodyLevel2DetailProvider baseDetailProvider,
            List<AbstractDesignBodyLevel2DetailProvider> perCombinationElementDetailProviders,
            ISpecificDesignBodyLevel2DetailsGenerator specificGenerator
            )
        {
            PerCombinationElementDetailProviders = perCombinationElementDetailProviders;
            BaseDetailProvider = baseDetailProvider;
            SpecificGenerator = specificGenerator;
        }
    }
}