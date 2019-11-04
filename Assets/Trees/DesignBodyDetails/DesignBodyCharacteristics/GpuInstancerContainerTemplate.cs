using System.Collections.Generic;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics
{
    public class GpuInstancerContainerTemplate
    {
        public GpuInstancerCommonData CommonData;
        public GpuInstancingUniformsArrayTemplate UniformsArrayTemplate;
        public AbstractDesignBodyLevel2DetailProvider DetailProvider;

        public GpuInstancerContainerTemplate(
            GpuInstancerCommonData commonData,
            GpuInstancingUniformsArrayTemplate uniformsArrayTemplate = null,
            AbstractDesignBodyLevel2DetailProvider detailProvider = null
        )
        {
            CommonData = commonData;
            var material = commonData.Material;
            if (!material.enableInstancing)
            {
                Debug.LogWarning("A material in gpuInstancerContainerTemplate had not enabled instancing");
                material.enableInstancing = true;
            }
            if (uniformsArrayTemplate == null)
            {
                uniformsArrayTemplate =
                    new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>());
            }
            UniformsArrayTemplate = uniformsArrayTemplate;
            DetailProvider = detailProvider;
        }
    }
}