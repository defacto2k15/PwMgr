using System.Collections.Generic;
using System.Linq;
using Assets.FinalExecution;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.Generation.ETree;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Trees.DesignBodyDetails
{
    public class EVegetationDetailProviderShifter 
    {
        private DetailProviderRepository _detailProviderRepository;
        private Mesh _quadBillboardMesh;
        private readonly FinalVegetationReferencedAssets _referencedAssets;

        public  EVegetationDetailProviderShifter (DetailProviderRepository detailProviderRepository, Mesh quadBillboardMesh, FinalVegetationReferencedAssets referencedAssets)
        {
            _detailProviderRepository = detailProviderRepository;
            _quadBillboardMesh = quadBillboardMesh;
            _referencedAssets = referencedAssets;
        }

        public Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>>
            CreateRepresentationsFromClanEnhanced(ETreeClanTemplate clan, VegetationSpeciesEnum speciesEnum, MainPlantDetailProviderDisposition disposition)
        {
            var outDictionary = new Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>>();

            outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.FULL)] =
                CreateRepresentationInstanceCombinationWithDifferentMeshesEx(clan.TreePyramids.Select(c => c.FullTreeMesh).ToList(), DitheringMode.FULL_DETAIL,
                    disposition.PerDetailDispositions[VegetationDetailLevel.FULL]);

            var hasSimplifiedVersion = clan.TreePyramids.Any(c => c.SimplifiedTreeMesh != null);
            if (hasSimplifiedVersion)
            {
                outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.REDUCED)] =
                    CreateRepresentationInstanceCombinationWithDifferentMeshesEx(clan.TreePyramids.Select(c => c.SimplifiedTreeMesh).ToList(),
                         DitheringMode.REDUCED_DETAIL, disposition.PerDetailDispositions[VegetationDetailLevel.REDUCED]);
            }

            var hasBillboardVersion = clan.TreePyramids.Any(c => c.BillboardTextureArray != null);
            if (hasBillboardVersion)
            {
                outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.BILLBOARD)] = CreateBillboardRepresentationEnhanced(
                    clan.TreePyramids.Select(c => c.BillboardTextureArray).ToList(), disposition.PerDetailDispositions[VegetationDetailLevel.BILLBOARD]);

            }

            return outDictionary;
        }

        private List<DesignBodyRepresentationInstanceCombination>
            CreateRepresentationInstanceCombinationWithDifferentMeshesEx(List<Mesh> allMeshes, DitheringMode ditheringMode, SingleDetailDisposition dispositionPerDetailDisposition)
        {
            var combinationsList = new List<DesignBodyRepresentationInstanceCombination>();

            Material material = new Material(Shader.Find("Custom/EVegetation/BaseTreeInstanced"));
            UniformsPack uniformsPack = new UniformsPack();
            uniformsPack.SetTexture("_MainTex", _referencedAssets.EVegetationMainTexture);
            foreach (var mesh in allMeshes)
            {
                var containerTemplates = new List<GpuInstancerContainerTemplate>
                {
                    new GpuInstancerContainerTemplate(
                        commonData: new GpuInstancerCommonData()
                        {
                            Mesh = mesh,
                            CastShadows = ShadowCastingMode.On,
                            Material = material,
                            SubmeshIndex = 0,
                            UniformsPack = uniformsPack
                        }, uniformsArrayTemplate: new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                        {
                            new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4)

                        }), detailProvider: _detailProviderRepository.CreateColorUniformDetailProvider(dispositionPerDetailDisposition))
                };
                var representationCombination = new DesignBodyRepresentationInstanceCombination(templates: containerTemplates,
                    commonDetailProvider: _detailProviderRepository.Common3DTreeDetailProvider(dispositionPerDetailDisposition));
                combinationsList.Add(representationCombination);
            }

            return new List<DesignBodyRepresentationInstanceCombination>(combinationsList);
        }

        private List<DesignBodyRepresentationInstanceCombination> CreateBillboardRepresentationEnhanced(List<EBillboardTextureArray> billboardArrays, SingleDetailDisposition disposition)
        {
            var material = new Material(Shader.Find("Custom/EVegetation/GenericBillboard.Instanced"));
            return billboardArrays.Select(collageTexture =>
                new DesignBodyRepresentationInstanceCombination(
                    templates: new List<GpuInstancerContainerTemplate>()
                    {
                        new GpuInstancerContainerTemplate(
                            commonData: new GpuInstancerCommonData()
                            {
                                Material = material,
                                Mesh = _quadBillboardMesh,
                                UniformsPack = CreateBillboardRepresentationUniformsPack(collageTexture),
                                SubmeshIndex = 0
                            },
                            uniformsArrayTemplate: new GpuInstancingUniformsArrayTemplate(
                                new List<GpuInstancingUniformTemplate>()
                                {
                                    new GpuInstancingUniformTemplate("_BaseYRotation", GpuInstancingUniformType.Float),
                                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4)
                                }),
                            detailProvider: _detailProviderRepository
                                .CalculateBillboardDesignBodyTreeScaleDetailProvider(
                                    new MyTransformTriplet(Vector3.zero, Quaternion.Euler(Vector3.zero),
                                        RescaleBillboardOffsets(collageTexture.ScaleOffsets)),
                                    disposition)),
                    },
                    commonDetailProvider: new ColorUniformDetailProvider(disposition),
                    specificGenerator:new BillboardSpecificDesignBodyLevel2DetailsGenerator(disposition, RescaleBillboardOffsets(collageTexture.ScaleOffsets))
                )).ToList();
        }


        private Vector3 RescaleBillboardOffsets(Vector3 input)
        {
            return new Vector3(input.x * 1.5f, input.y, input.z);
        }

        private UniformsPack CreateBillboardRepresentationUniformsPack(EBillboardTextureArray collageTexture)
        {
            var uniforms = new UniformsPack();
            uniforms.SetTexture("_CollageTextureArray", collageTexture.Array);
            uniforms.SetUniform("_ImagesInArrayCount", collageTexture.Array.depth);
            return uniforms;
        }
    }
}