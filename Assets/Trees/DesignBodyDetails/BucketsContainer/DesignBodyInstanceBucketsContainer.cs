using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails.BucketsContainer
{
    public class DesignBodyInstanceBucketsContainer
    {
        private GlobalGpuInstancingContainer _instancingContainer;

        private Dictionary<DesignBodyRepresentationQualifier, DesignBodyBucketIds> _bucketIds =
            new Dictionary<DesignBodyRepresentationQualifier, DesignBodyBucketIds>();

        public DesignBodyInstanceBucketsContainer(GlobalGpuInstancingContainer instancingContainer)
        {
            _instancingContainer = instancingContainer;
        }

        public void InitializeLists( Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>> allRepresentations)
        {
            foreach (var pair in allRepresentations)
            {
                DesignBodyRepresentationQualifier representationQualifier = pair.Key;
                var combinations = pair.Value;

                var perRepresentationIds = new List<DesignBodyCombinationBucketIds>();
                foreach (var combination in combinations)
                {
                    var perCombinationIds = new List<int>();
                    foreach (var template in combination.Templates)
                    {
                        var bucketId = _instancingContainer.CreateBucket(new GpuInstancingVegetationSubjectContainer(
                            template.CommonData, template.UniformsArrayTemplate));
                        perCombinationIds.Add(bucketId);
                    }
                    perRepresentationIds.Add(new DesignBodyCombinationBucketIds()
                    {
                        Ids = perCombinationIds
                    });
                }
                _bucketIds[representationQualifier] = new DesignBodyBucketIds()
                {
                    PerCombinationBucketIds = perRepresentationIds
                };
            }
        }

        private class DesignBodyBucketIds
        {
            public List<DesignBodyCombinationBucketIds> PerCombinationBucketIds;
        }

        private class DesignBodyCombinationBucketIds
        {
            public List<int> Ids; 
        }

        public int RetriveCombinationCountFor(DesignBodyRepresentationQualifier qualifier)
        {
            Preconditions.Assert(_bucketIds.ContainsKey(qualifier), () => "No bucket for qualifier: "+qualifier.DetailLevel+" "+qualifier.SpeciesEnum);
            return _bucketIds[qualifier].PerCombinationBucketIds.Count;
        }

        public RepresentationCombinationInstanceId AddInstance( DesignBodyRepresentationQualifier qualifier, int selectedCombinationIdx, DesignBodyLevel2DetailContainerForCombination details)
        {
            var outGpuIds = new List<GpuBucketedInstanceId>();
            foreach (var aPair in _bucketIds[qualifier].PerCombinationBucketIds[selectedCombinationIdx].Ids
                .Select((c, i) => new
                {
                    Id = c,
                    Index = i
                }))
            {
                var detail = details.GetDetailFor(aPair.Index);
                var matrix = detail.TransformTriplet.ToLocalToWorldMatrix();
                var uniformsPack = detail.RetriveUniformsPack();

                var gpuId = _instancingContainer.AddInstance(aPair.Id, matrix, uniformsPack);
                outGpuIds.Add(gpuId);
            }
            return new RepresentationCombinationInstanceId(outGpuIds);
        }

        public void ModifyInstance(RepresentationCombinationInstanceId gpuIds,  DesignBodyRepresentationQualifier qualifier, int selectedCombinationIdx, DesignBodyLevel2DetailContainerForCombination details )
        {

            foreach (var aPair in _bucketIds[qualifier].PerCombinationBucketIds[selectedCombinationIdx].Ids
                .Select((c, i) => new
                {
                    Id = c,
                    Index = i
                }))
            {
                var detail = details.GetDetailFor(aPair.Index);
                var matrix = detail.TransformTriplet.ToLocalToWorldMatrix();
                var uniformsPack = detail.RetriveUniformsPack();

                _instancingContainer.ModifyInstance(gpuIds.InstanceIds[aPair.Index], matrix, uniformsPack);
            }
        }

        public void RemoveInstance(RepresentationCombinationInstanceId instanceId)
        {
            foreach (var id in instanceId.InstanceIds)
            {
                _instancingContainer.RemoveInstance(id);
            }
        }
    }

}