using System.Collections.Generic;
using Assets.Random;
using Assets.Repositioning;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.DesignBodyDetails.MyRandom;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;

namespace Assets.Trees.DesignBodyDetails
{
    public class DesignBodyPortrayalForger : IDesignBodyPortrayalForger
    {
        private DesignBodyRepresentationContainer _representationContainer;
        private DesignBodyInstanceBucketsContainer _instanceBucketsContainer;
        private readonly Repositioner _repositioner;
        private MyRandomProvider _randomProvider;

        public DesignBodyPortrayalForger(
            DesignBodyRepresentationContainer representationContainer,
            DesignBodyInstanceBucketsContainer instanceBucketsContainer,
            Repositioner repositioner = null)
        {
            _representationContainer = representationContainer;
            _instanceBucketsContainer = instanceBucketsContainer;
            _repositioner = repositioner;
            _randomProvider = new MyRandomProvider(1, true);
        }

        public RepresentationCombinationInstanceId Forge( DesignBodyLevel1DetailWithSpotModification level1DetailWithSpotModification)
        {
            var (qualifier, representationIdx, level2DetailsCombination) = CalculateInfoForAddingInstance(level1DetailWithSpotModification.Level1Detail);
            if (level1DetailWithSpotModification.SpotModification != null)
            {
                DesignBodyLevel2Detail additionalLevel2Details = level1DetailWithSpotModification.SpotModification.GenerateLevel2Details();
                level2DetailsCombination = level2DetailsCombination.MergeWithAdditionalDetails(additionalLevel2Details);
            }

            return _instanceBucketsContainer.AddInstance(qualifier, representationIdx, level2DetailsCombination);
        }

        private (DesignBodyRepresentationQualifier qualifier, int selectedCombinationIdx, DesignBodyLevel2DetailContainerForCombination details)
            CalculateInfoForAddingInstance(DesignBodyLevel1Detail level1Detail)
        {
            var qualifier = new DesignBodyRepresentationQualifier(level1Detail.SpeciesEnum, level1Detail.DetailLevel);
            var combinationsCount = _instanceBucketsContainer.RetriveCombinationCountFor(qualifier);
            _randomProvider.SetGlobalSeed(level1Detail.Seed);
            var selectedCombinationIdx = _randomProvider.NextWithMax(StringSeed.CombinationIdx, 0, combinationsCount - 1);
            var combinationCharacteristics = _representationContainer.RetriveDetailProviderFor(qualifier, selectedCombinationIdx);
            var details = CreateDetails(combinationCharacteristics, level1Detail, _randomProvider);
            return (qualifier, selectedCombinationIdx, details);
        }

        private DesignBodyLevel2DetailContainerForCombination CreateDetails(
            DesignBodyRepresentationCombinationCharacteristics providersCombination,
            DesignBodyLevel1Detail level1Detail,
            MyRandomProvider randomProvider)
        {
            var baseDetail = new DesignBodyLevel2Detail();
            providersCombination.BaseDetailProvider.AddDetailsFor(baseDetail, level1Detail, randomProvider);

            var createdDetails = new List<DesignBodyLevel2Detail>(2);
            var count = providersCombination.PerCombinationElementDetailProviders.Count;
            for (int i = 0; i < count; i++)
            {
                DesignBodyLevel2Detail detail = null;
                if (i == count - 1)
                {
                    detail = baseDetail;
                }
                else
                {
                    detail = baseDetail.Clone();
                }
                var perElementProvider = providersCombination.PerCombinationElementDetailProviders[i];

                perElementProvider?.AddDetailsFor(detail, level1Detail, randomProvider);
                createdDetails.Add(detail);
            }

            if (_repositioner != null)
            {
                createdDetails.ForEach(c => c.Position = _repositioner.Move(c.Position));
            }
            return new DesignBodyLevel2DetailContainerForCombination(createdDetails);
        }

        public void Modify(RepresentationCombinationInstanceId combinationInstanceId, DesignBodyLevel1DetailWithSpotModification level1DetailWithSpotModification)
        {
            var (qualifier, representationIdx, level2DetailsCombination) = CalculateInfoForAddingInstance(level1DetailWithSpotModification.Level1Detail);
            if (level1DetailWithSpotModification.SpotModification != null)
            {
                DesignBodyLevel2Detail additionalLevel2Details = level1DetailWithSpotModification.SpotModification.GenerateLevel2Details();
                level2DetailsCombination = level2DetailsCombination.MergeWithAdditionalDetails(additionalLevel2Details);
            }
            _instanceBucketsContainer.ModifyInstance(combinationInstanceId, qualifier, representationIdx, level2DetailsCombination);
        }

        public void Remove(RepresentationCombinationInstanceId instanceId)
        {
            _instanceBucketsContainer.RemoveInstance(instanceId);
        }

        private class DesignBodyPortrayalForgerObjectCache
        {
            public MyRandomProvider RandomProvider;
        }
    }
}