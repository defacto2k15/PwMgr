using System.Collections.Generic;
using System.Linq;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.RuntimeManagement;
using Assets.Utils;

namespace Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics
{
    public class DesignBodyRepresentationContainer
    {
        private Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationCombinationCharacteristics>>
            _representationCombinationCharacteristics = new Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationCombinationCharacteristics>>();

        public void InitializeLists( Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>> allRepresentations)
        {
            foreach (var pair in allRepresentations)
            {
                DesignBodyRepresentationQualifier representationQualifier = pair.Key;
                var combinations = pair.Value;
                _representationCombinationCharacteristics[representationQualifier] =
                    new List<DesignBodyRepresentationCombinationCharacteristics>();
                foreach (var combination in combinations)
                {
                    _representationCombinationCharacteristics[representationQualifier].Add(
                        new DesignBodyRepresentationCombinationCharacteristics(
                            combination.CommonDetailProvider,
                            combination.Templates.Select(c => c.DetailProvider).ToList(),
                            combination.SpecificGenerator));
                }
            }
        }

        public DesignBodyRepresentationCombinationCharacteristics RetriveDetailProviderFor( DesignBodyRepresentationQualifier qualifier, int combinationIndex)
        {
            return _representationCombinationCharacteristics[qualifier][combinationIndex];
        }

        public int RetriveCombinationCountFor(DesignBodyRepresentationQualifier qualifier)
        {
            Preconditions.Assert(_representationCombinationCharacteristics.ContainsKey(qualifier), 
                () => "No combinationfor qualifier: "+qualifier.DetailLevel+" "+qualifier.SpeciesEnum);
            return _representationCombinationCharacteristics[qualifier].Count;
        }
    }
}