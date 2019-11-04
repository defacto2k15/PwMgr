using System.Collections.Generic;
using System.Linq;
using Assets.FinalExecution;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.Generation;
using Assets.Utils;

namespace Assets.Trees.DesignBodyDetails
{
    public interface IDesignBodyRepresentationInstanceCombinationProvider
    {
        Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>> CreateRepresentations(
            RootSinglePlantShiftingConfiguration shiftingConfiguration, VegetationSpeciesEnum speciesEnum);
    }

    public class GDesignBodyRepresentationInstanceCombinationProvider : IDesignBodyRepresentationInstanceCombinationProvider
    {
        private TreeFileManager _fileManager;
        private GTreeDetailProviderShifter _shifter;

        public GDesignBodyRepresentationInstanceCombinationProvider(TreeFileManager fileManager, GTreeDetailProviderShifter shifter)
        {
            _fileManager = fileManager;
            _shifter = shifter;
        }

        public Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>> CreateRepresentations(
            RootSinglePlantShiftingConfiguration shiftingConfiguration, VegetationSpeciesEnum speciesEnum)
        {

            if (shiftingConfiguration.ClanName != null)
            {
                return _shifter.CreateRepresentations(_fileManager.LoadTreeClan(shiftingConfiguration.ClanName), speciesEnum,
                    shiftingConfiguration.PlantDetailProviderDisposition);
            }
            else
            {
                return _shifter.CreateRepresentations(_fileManager.LoadTreeClanFromLivePrefab(shiftingConfiguration.LivePrefabPath), speciesEnum,
                    shiftingConfiguration.PlantDetailProviderDisposition);
            }
        }
    }


    public class EVegetationDesignBodyRepresentationInstanceCombinationProvider : IDesignBodyRepresentationInstanceCombinationProvider
    {
        private TreePrefabManager _prefabManager;
        private EVegetationDetailProviderShifter _shifter;
        private FinalVegetationReferencedAssets _referencedAssets;

        public EVegetationDesignBodyRepresentationInstanceCombinationProvider(TreePrefabManager prefabManager, EVegetationDetailProviderShifter shifter, FinalVegetationReferencedAssets referencedAssets)
        {
            _prefabManager = prefabManager;
            _shifter = shifter;
            _referencedAssets = referencedAssets;
        }

        public Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>> CreateRepresentations(
            RootSinglePlantShiftingConfiguration shiftingConfiguration, VegetationSpeciesEnum speciesEnum)
        {
            ETreeClanTemplate treeClanTemplate;
            var prefabName = shiftingConfiguration.EVegetationPrefabName;
            if (prefabName != null)
            {
                var templatePrefab = _referencedAssets.Prefabs.FirstOrDefault(c => c.name.Equals(prefabName));
                Preconditions.Assert(templatePrefab != null, $"Cannot find a prefab template of name {prefabName}");
                treeClanTemplate = _prefabManager.ELoadTreeClanFromLivePrefab(templatePrefab);
            }
            else
            {
                treeClanTemplate = _prefabManager.ELoadCompleteTreeClan(shiftingConfiguration.ClanName);
            }

            return _shifter.CreateRepresentationsFromClanEnhanced(treeClanTemplate, speciesEnum, shiftingConfiguration.PlantDetailProviderDisposition);
        }
    }


}