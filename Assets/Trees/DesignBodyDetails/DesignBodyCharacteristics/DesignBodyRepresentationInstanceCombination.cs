using System.Collections.Generic;
using Assets.Trees.DesignBodyDetails.DetailProvider;

namespace Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics
{
    public class DesignBodyRepresentationInstanceCombination
    {
        public List<GpuInstancerContainerTemplate> Templates;
        public AbstractDesignBodyLevel2DetailProvider CommonDetailProvider;
        public ISpecificDesignBodyLevel2DetailsGenerator SpecificGenerator;

        public DesignBodyRepresentationInstanceCombination(List<GpuInstancerContainerTemplate> templates,
            AbstractDesignBodyLevel2DetailProvider commonDetailProvider = null, ISpecificDesignBodyLevel2DetailsGenerator specificGenerator=null)
        {
            Templates = templates;
            CommonDetailProvider = commonDetailProvider;
            SpecificGenerator = specificGenerator;
        }
    }
}