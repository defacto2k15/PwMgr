using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class TreeClanToDetailProviderShifter //TODO DETELE IT
    {
        private DetailProviderRepository _detailProviderRepository;
        private Mesh _quadBillboardMesh;
        private DesignBodyRepresentationContainer _representationsContainer;
        private readonly DesignBodyInstanceBucketsContainer _instanceBucketsContainer;

        public TreeClanToDetailProviderShifter(DetailProviderRepository detailProviderRepository,
            Mesh quadBillboardMesh, DesignBodyRepresentationContainer representationContainer,
            DesignBodyInstanceBucketsContainer instanceBucketsContainer)
        {
            _detailProviderRepository = detailProviderRepository;
            _quadBillboardMesh = quadBillboardMesh;
            _representationsContainer = representationContainer;
            _instanceBucketsContainer = instanceBucketsContainer;
        }


        public void AddClan(TreeClan clan, VegetationSpeciesEnum speciesEnum)
        {
            var representationsFromClan = CreateRepresentationsFromClan(clan, speciesEnum);
            _representationsContainer.InitializeLists(representationsFromClan);
            _instanceBucketsContainer.InitializeLists(representationsFromClan);
        }

        private Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>>
            CreateRepresentationsFromClan(TreeClan clan, VegetationSpeciesEnum speciesEnum)
        {
            var outDictionary =
                new Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>>();
            outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.FULL)] =
                CreateFullDetailRepresentationInstanceCombination(clan);
            outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.REDUCED)] =
                CreateReducedDetailRepresentationInstanceCombination(clan);
            outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.BILLBOARD)] =
                CreateBillboardRepresentation(clan);

            return outDictionary;
        }

        private List<DesignBodyRepresentationInstanceCombination> CreateFullDetailRepresentationInstanceCombination(
            TreeClan clan)
        {
            return CreateRepresentationInstanceCombinationWithDifferentMeshes(
                clan.Pyramids.Select(c => c.FullDetailTree.gameObject.GetComponent<MeshFilter>().sharedMesh).ToList(),
                clan.Pyramids[0].FullDetailTree.gameObject.GetComponent<MeshRenderer>().sharedMaterials[0],
                clan.Pyramids[0].FullDetailTree.gameObject.GetComponent<MeshRenderer>().sharedMaterials[1],
                DitheringMode.FULL_DETAIL);
        }

        private List<DesignBodyRepresentationInstanceCombination> CreateReducedDetailRepresentationInstanceCombination(
            TreeClan clan)
        {
            return CreateRepresentationInstanceCombinationWithDifferentMeshes(
                clan.Pyramids.Select(c => c.SimplifiedTree.gameObject.GetComponent<MeshFilter>().sharedMesh).ToList(),
                clan.Pyramids[0].SimplifiedTree.gameObject.GetComponent<MeshRenderer>().sharedMaterials[0],
                clan.Pyramids[0].SimplifiedTree.gameObject.GetComponent<MeshRenderer>().sharedMaterials[1],
                DitheringMode.REDUCED_DETAIL);
        }

        private List<DesignBodyRepresentationInstanceCombination>
            CreateRepresentationInstanceCombinationWithDifferentMeshes(
                List<Mesh> allMeshes, Material oldBarkMaterial, Material oldLeafMaterial, DitheringMode ditheringMode)
        {
            var constantBarkUniforms = new UniformsPack();
            constantBarkUniforms.SetTexture("_MainTex", oldBarkMaterial.GetTexture("_MainTex"));
            constantBarkUniforms.SetTexture("_BumpSpecMap", oldBarkMaterial.GetTexture("_BumpSpecMap"));
            constantBarkUniforms.SetTexture("_TranslucencyMap", oldBarkMaterial.GetTexture("_TranslucencyMap"));
            constantBarkUniforms.SetUniform("_DitheringMode",
                DitheringModeUtils.RetriveDitheringModeIndex(ditheringMode));

            var constantLeafUniforms = new UniformsPack();
            constantLeafUniforms.SetTexture("_MainTex", oldLeafMaterial.GetTexture("_MainTex"));
            constantLeafUniforms.SetTexture("_ShadowTex", oldLeafMaterial.GetTexture("_ShadowTex"));
            constantLeafUniforms.SetTexture("_BumpSpecMap", oldLeafMaterial.GetTexture("_BumpSpecMap"));
            constantLeafUniforms.SetTexture("_TranslucencyMap", oldLeafMaterial.GetTexture("_TranslucencyMap"));
            constantLeafUniforms.SetUniform("_DitheringMode",
                DitheringModeUtils.RetriveDitheringModeIndex(ditheringMode));

            var leafInstancingMaterial = new Material(Shader.Find("Custom/Nature/Tree Creator Leaves Optimized Ugly"));
            var barkInstancingMaterial = new Material(Shader.Find("Custom/Nature/Tree Creator Bark Optimized"));
            leafInstancingMaterial.enableInstancing = true;
            barkInstancingMaterial.enableInstancing = true;


            List<DesignBodyRepresentationInstanceCombination> combinationsList =
                new List<DesignBodyRepresentationInstanceCombination>();
            foreach (var mesh in allMeshes)
            {
                DesignBodyRepresentationInstanceCombination representationCombination;
                if (mesh.subMeshCount == 2)
                {
                    representationCombination = new DesignBodyRepresentationInstanceCombination(
                        templates: new List<GpuInstancerContainerTemplate>()
                        {
                            new GpuInstancerContainerTemplate(
                                commonData: new GpuInstancerCommonData()
                                {
                                    Material = barkInstancingMaterial,
                                    Mesh = mesh,
                                    UniformsPack = constantBarkUniforms,
                                    SubmeshIndex = 0
                                }),
                            new GpuInstancerContainerTemplate(
                                new GpuInstancerCommonData()
                                {
                                    Material = leafInstancingMaterial,
                                    Mesh = mesh,
                                    UniformsPack = constantLeafUniforms,
                                    SubmeshIndex = 1
                                })
                        },
                        commonDetailProvider: _detailProviderRepository.Common3DTreeDetailProvider());
                }
                else
                {
                    representationCombination = new DesignBodyRepresentationInstanceCombination(
                        templates: new List<GpuInstancerContainerTemplate>()
                        {
                            new GpuInstancerContainerTemplate(
                                commonData: new GpuInstancerCommonData()
                                {
                                    Material = leafInstancingMaterial,
                                    Mesh = mesh,
                                    UniformsPack = constantLeafUniforms,
                                    SubmeshIndex = 0
                                }),
                        },
                        commonDetailProvider: _detailProviderRepository.Common3DTreeDetailProvider());
                }
                combinationsList.Add(representationCombination);
            }

            return new List<DesignBodyRepresentationInstanceCombination>(combinationsList);
        }

        private List<DesignBodyRepresentationInstanceCombination> CreateBillboardRepresentation(TreeClan clan)
        {
            var material = new Material(Shader.Find("Custom/Vegetation/GenericBillboard.Instanced"));
            var ax = clan.Pyramids.Select(c => c.CollageTexture).Select(collageTexture =>
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
                                }),
                            detailProvider: _detailProviderRepository
                                .CalculateBillboardDesignBodyTreeScaleDetailProvider(new MyTransformTriplet(
                                    Vector3.zero, Quaternion.Euler(Vector3.zero),
                                    RescaleBillboardOffsets(collageTexture.ScaleOffsets)))),
                    },
                    commonDetailProvider: new IdentityDesignBodyLevel2DetailProvider(),
                    specificGenerator: new BillboardSpecificDesignBodyLevel2DetailsGenerator(null,RescaleBillboardOffsets(collageTexture.ScaleOffsets))
                )).ToList();
            return ax;
        }

        private Vector3 RescaleBillboardOffsets(Vector3 input)
        {
            return new Vector3(input.x * 1.5f, input.y, input.z);
        }

        private UniformsPack CreateBillboardRepresentationUniformsPack(BillboardCollageTexture collageTexture)
        {
            var uniforms = new UniformsPack();
            uniforms.SetTexture("_CollageTex", collageTexture.Collage);
            uniforms.SetUniform("_BillboardCount", collageTexture.SubTexturesCount);
            uniforms.SetUniform("_ColumnsCount", collageTexture.ColumnsCount);
            uniforms.SetUniform("_RowsCount", collageTexture.LinesCount);
            return uniforms;
        }
    }
}