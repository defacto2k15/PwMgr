using System.Collections.Generic;
using Assets.Trees.DesignBodyDetails.MyRandom;

namespace Assets.Trees.DesignBodyDetails.DetailProvider
{
    public class DesignBodyLevel2DetailProviderComposition : AbstractDesignBodyLevel2DetailProvider
    {
        public DesignBodyLevel2DetailProviderComposition(List<AbstractDesignBodyLevel2DetailProvider> providersList)
        {
            _providersList = providersList;
        }

        private List<AbstractDesignBodyLevel2DetailProvider> _providersList;

        public override void AddDetailsFor(DesignBodyLevel2Detail level2Detail, DesignBodyLevel1Detail level1Detail,
            MyRandomProvider randomGenerator)
        {
            foreach (var aProvider in _providersList)
            {
                aProvider.AddDetailsFor(level2Detail, level1Detail, randomGenerator);
            }
        }
    }
}