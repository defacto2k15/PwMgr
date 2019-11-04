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
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Trees.DesignBodyDetails
{
    public class GTreeDetailProviderShifter 
    {
        private DetailProviderRepository _detailProviderRepository;
        private Mesh _quadBillboardMesh;

        public GTreeDetailProviderShifter(DetailProviderRepository detailProviderRepository, Mesh quadBillboardMesh)
        {
            _detailProviderRepository = detailProviderRepository;
            _quadBillboardMesh = quadBillboardMesh;
        }

        public Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>> CreateRepresentations(TreeClanEnhanced clan, VegetationSpeciesEnum speciesEnum,
            MainPlantDetailProviderDisposition disposition)
        {
            Preconditions.Assert(ClanHasAtLeastOneTexture(clan), $"Clan of species {speciesEnum} does not have even one texture" );
            var representationsFromClan = CreateRepresentationsFromClanEnhanced(clan, speciesEnum, disposition);
            return representationsFromClan;
        }

        private bool ClanHasAtLeastOneTexture(TreeClanEnhanced clan)
        {
            var texPack = clan.TreeTexturesPack;
            return texPack.BarkBumpSpecMap != null
                || texPack.BarkMainTex != null
                || texPack.BarkTranslucencyMap != null
                || texPack.LeafMainTex != null
                || texPack.LeafBumpSpecMap != null
                || texPack.LeafShadowTex != null
                || texPack.LeafTranslucencyMap != null;
        }

        private Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>>
            CreateRepresentationsFromClanEnhanced(
                TreeClanEnhanced clan,
                VegetationSpeciesEnum speciesEnum,
                MainPlantDetailProviderDisposition disposition)
        {
            var outDictionary =
                new Dictionary<DesignBodyRepresentationQualifier, List<DesignBodyRepresentationInstanceCombination>>();

            outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.FULL)] =
                CreateRepresentationInstanceCombinationWithDifferentMeshesEx(
                    clan.Pyramids.Select(c => c.FullDetailMesh).ToList(), clan.TreeTexturesPack,
                    DitheringMode.FULL_DETAIL,
                    disposition.PerDetailDispositions[VegetationDetailLevel.FULL]);

            if (clan.HasSimplifiedVersion)
            {
                outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.REDUCED)] =
                    CreateRepresentationInstanceCombinationWithDifferentMeshesEx(
                        clan.Pyramids.Select(c => c.FullDetailMesh).ToList(), clan.TreeTexturesPack,
                        DitheringMode.REDUCED_DETAIL,
                        disposition.PerDetailDispositions[VegetationDetailLevel.REDUCED]);
            }

            if (clan.HasBillboard)
            {
                outDictionary[new DesignBodyRepresentationQualifier(speciesEnum, VegetationDetailLevel.BILLBOARD)] =
                    CreateBillboardRepresentationEnhanced(clan,
                        disposition.PerDetailDispositions[VegetationDetailLevel.BILLBOARD]);
            }


            return outDictionary;
        }


        private List<DesignBodyRepresentationInstanceCombination>
            CreateRepresentationInstanceCombinationWithDifferentMeshesEx(List<Mesh> allMeshes,
                TreeTexturesPack texturesPack, DitheringMode ditheringMode,
                SingleDetailDisposition dispositionPerDetailDisposition)
        {
            List<DesignBodyRepresentationInstanceCombination> combinationsList =
                new List<DesignBodyRepresentationInstanceCombination>();

            foreach (var mesh in allMeshes)
            {
                var containerTemplates = new List<GpuInstancerContainerTemplate>();
                int submeshIndex = 0;
                if (texturesPack.BarkMainTex != null)
                {
                    var constantBarkUniforms = new UniformsPack();
                    constantBarkUniforms.SetTexture("_MainTex", texturesPack.BarkMainTex);
                    constantBarkUniforms.SetTexture("_BumpSpecMap", texturesPack.BarkBumpSpecMap);
                    constantBarkUniforms.SetTexture("_TranslucencyMap", texturesPack.BarkTranslucencyMap);
                    constantBarkUniforms.SetUniform("_DitheringMode",
                        DitheringModeUtils.RetriveDitheringModeIndex(ditheringMode));
                    var barkInstancingMaterial = new Material(Shader.Find("Custom/Nature/Tree Creator Bark Optimized"));
                    barkInstancingMaterial.enableInstancing = true;

                    containerTemplates.Add(new GpuInstancerContainerTemplate(
                        commonData: new GpuInstancerCommonData()
                        {
                            Material = barkInstancingMaterial,
                            Mesh = mesh,
                            UniformsPack = constantBarkUniforms,
                            SubmeshIndex = submeshIndex,
                            CastShadows = ShadowCastingMode.On
                        }));
                    submeshIndex++;
                }

                if (texturesPack.LeafMainTex != null)
                {
                    var constantLeafUniforms = new UniformsPack();
                    constantLeafUniforms.SetTexture("_MainTex", texturesPack.LeafMainTex);
                    constantLeafUniforms.SetTexture("_ShadowTex", texturesPack.LeafShadowTex);
                    constantLeafUniforms.SetTexture("_BumpSpecMap", texturesPack.LeafBumpSpecMap);
                    constantLeafUniforms.SetTexture("_TranslucencyMap", texturesPack.LeafTranslucencyMap);
                    constantLeafUniforms.SetUniform("_DitheringMode",
                        DitheringModeUtils.RetriveDitheringModeIndex(ditheringMode));

                    var leafInstancingMaterial =
                        new Material(Shader.Find("Custom/Nature/Tree Creator Leaves Optimized Ugly"));
                    leafInstancingMaterial.enableInstancing = true;

                    containerTemplates.Add(
                        new GpuInstancerContainerTemplate(
                            new GpuInstancerCommonData()
                            {
                                Material = leafInstancingMaterial,
                                Mesh = mesh,
                                UniformsPack = constantLeafUniforms,
                                SubmeshIndex = submeshIndex++,
                                CastShadows = ShadowCastingMode.On
                            },
                            uniformsArrayTemplate: new GpuInstancingUniformsArrayTemplate(
                                new List<GpuInstancingUniformTemplate>()
                                {
                                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4)
                                }),
                            detailProvider: _detailProviderRepository.CreateColorUniformDetailProvider(
                                dispositionPerDetailDisposition)));


                    var representationCombination = new DesignBodyRepresentationInstanceCombination(
                        templates: containerTemplates,
                        commonDetailProvider: _detailProviderRepository.Common3DTreeDetailProvider(
                            dispositionPerDetailDisposition));

                    combinationsList.Add(representationCombination);
                    submeshIndex++;
                }
            }

            return new List<DesignBodyRepresentationInstanceCombination>(combinationsList);
        }

        private List<DesignBodyRepresentationInstanceCombination> CreateBillboardRepresentationEnhanced(
            TreeClanEnhanced clan, SingleDetailDisposition disposition)
        {
            var material = new Material(Shader.Find("Custom/Vegetation/GenericBillboard.Instanced"));
            return clan.Pyramids.Select(c => c.CollageTexture).Select(collageTexture =>
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