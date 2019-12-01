using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Assets.FinalExecution;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation.Configurations;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Ring2.Stamping;
using Assets.Scheduling;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.ESurface
{
    public class ESurfaceSinglePatchDebugGO : MonoBehaviour
    {
        public Vector4 PatchArea;
        public GameObject DummyPlate1;
        public GameObject DummyPlate2;
        public bool GenerateDebugGrid;
        private ESurfacePatchProvider _provider;
        private UltraUpdatableContainer _updatableContainer;
        private GRing2PatchesCreatorProxy _patchesCreatorProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var shaderContainerGO = FindObjectOfType<ComputeShaderContainerGameObject>();

            var configuration = new FEConfiguration(new FilePathsConfiguration());
            GlobalServicesProfileInfo servicesProfileInfo = new GlobalServicesProfileInfo();
            var ultraUpdatableContainer = new UltraUpdatableContainer(
                configuration.SchedulerConfiguration,
                servicesProfileInfo, 
                configuration.UpdatableContainerConfiguration);
            Dictionary<int, float> intensityPatternPixelsPerUnit = new Dictionary<int, float>()
            {
                {1,1 }
            };
            int mipmapLevelToExtract = 2;
            Dictionary<int, float> plateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                {1, 5 }
            };
            _updatableContainer = new UltraUpdatableContainer(new MyUtSchedulerConfiguration(), new GlobalServicesProfileInfo(), new UltraUpdatableContainerConfiguration());
            _provider = ESurfaceProviderInitializationHelper.ConstructProvider(
                _updatableContainer, intensityPatternPixelsPerUnit, shaderContainerGO, mipmapLevelToExtract, plateStampPixelsPerUnit);

            _patchesCreatorProxy =  new GRing2PatchesCreatorProxy(ESurfaceProviderInitializationHelper.CreateRing2PatchesCreator(_updatableContainer, intensityPatternPixelsPerUnit));

            RegeneratePatch();
        }

        public void RegeneratePatch()
        {
            if (GenerateDebugGrid)
            {
                var root = new GameObject("rootGrid");
                var segmentLength = 90;
                for (int x = -3; x < 3; x++)
                {
                    for (int y = -3; y < 3; y++)
                    {
                        var inGamePosition = new MyRectangle(x*segmentLength, y*segmentLength, segmentLength, segmentLength);
                        var flatLod = new FlatLod(1, 0);
                        var detailPack = _provider.ProvideSurfaceDetailAsync(inGamePosition, flatLod).Result;

                        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        //StandardMaterialUtils.SetMaterialRenderingModeToAlphablend(quad.GetComponent<MeshRenderer>().material);
                        quad.name = $"Plate {x} : {y}";
                        quad.transform.rotation = Quaternion.Euler(90,0,0);
                        quad.transform.localScale = new Vector3(segmentLength,segmentLength,1);
                        quad.transform.localPosition = new Vector3(inGamePosition.X + inGamePosition.Width / 2,1, inGamePosition.Y + inGamePosition.Height / 2);
                        quad.transform.SetParent(root.transform);
                        if (detailPack != null)
                        {
                            quad.GetComponent<MeshRenderer>().GetComponent<MeshRenderer>().material.mainTexture = detailPack.MainTexture;
                        }
                        else
                        {
                            GameObject.Destroy(quad);
                        }
                    }
                }
            }
            else
            {
                MyRectangle inGamePosition = new MyRectangle(PatchArea.x, PatchArea.y, PatchArea.z - PatchArea.x, PatchArea.w - PatchArea.y);
                var flatLod = new FlatLod(1, 0);

                var detailPack = _provider.ProvideSurfaceDetailAsync(inGamePosition, flatLod).Result;
                DummyPlate1.GetComponent<MeshRenderer>().material.mainTexture = detailPack.MainTexture;
                //DummyPlate2.GetComponent<MeshRenderer>().material.SetTexture("_BumpMap", detailPack.NormalTexture);

                //var devisedPatches = _patchesCreatorProxy.CreatePatchAsync(inGamePosition.ToRectangle(), flatLod.ScalarValue).Result;
                //var onlyPatch = devisedPatches.First();

                //var repositioner = Repositioner.Default; //todo
                //var templates = new List<Ring2PlateStampTemplate>();
                //foreach (var sliceInfo in onlyPatch.SliceInfos)
                //{
                //    var propertyBlock = sliceInfo.Uniforms.ToPropertyBlockTemplate();

                //    templates.Add(new Ring2PlateStampTemplate( new MaterialTemplate(
                //            Ring2ShaderNames.RuntimeTerrainTexture, sliceInfo.Keywords, propertyBlock), repositioner.Move(onlyPatch.SliceArea), flatLod.ScalarValue));
                //}

                //var material = Ring2PlateStamper_CreateRenderMaterial(templates.First());
                //material.EnableKeyword("GENERATE_COLOR");
                //DummyPlate2.GetComponent<MeshRenderer>().material = material;
            }
        }

        private Material Ring2PlateStamper_CreateRenderMaterial(Ring2PlateStampTemplate template)
        {
            var renderMaterial = new Material(Shader.Find("Custom/Terrain/Ring2Stamper"));
            foreach (var keyword in template.MaterialTemplate.KeywordSet.Keywords)
            {
                renderMaterial.EnableKeyword(keyword);
            }
            template.MaterialTemplate.PropertyBlock.FillMaterial(renderMaterial);

            renderMaterial.SetVector("_Coords",
                new Vector4(
                    template.PlateCoords.X,
                    template.PlateCoords.Y,
                    template.PlateCoords.Width,
                    template.PlateCoords.Height
                ));
            return renderMaterial;
        }

    }


    public static class ESurfaceProviderInitializationHelper
    {
        public static ESurfacePatchProvider ConstructProvider(UltraUpdatableContainer updatableContainer, Dictionary<int, float> intensityPatternPixelsPerUnit,
            ComputeShaderContainerGameObject shaderContainerGO, int mipmapLevelToExtract, Dictionary<int, float> plateStampPixelsPerUnit)
        {
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();
            TextureConcieverUTProxy conciever = new TextureConcieverUTProxy();
            updatableContainer.Add(conciever);

            var ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(
                new Ring2PatchesPainter(
                    new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));
            updatableContainer.Add(ring2PatchesPainterUtProxy);

            UTRing2PlateStamperProxy stamperProxy = new UTRing2PlateStamperProxy(
                new Ring2PlateStamper(new Ring2PlateStamperConfiguration()
                {
                    PlateStampPixelsPerUnit = plateStampPixelsPerUnit
                }, shaderContainerGO));
            updatableContainer.Add(stamperProxy);

            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(shaderContainerGO), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(500, 500)
                }));
            updatableContainer.Add(textureRendererProxy);

            CommonExecutorUTProxy commonExecutorUtProxy = new CommonExecutorUTProxy(); //todo
            updatableContainer.Add(commonExecutorUtProxy);

            Ring2PatchStamplingOverseerFinalizer patchStamperOverseerFinalizer =
                new Ring2PatchStamplingOverseerFinalizer(stamperProxy, textureRendererProxy, commonExecutorUtProxy);

            MipmapExtractor mipmapExtractor = new MipmapExtractor(textureRendererProxy);
            var patchesCreatorProxy = new GRing2PatchesCreatorProxy(CreateRing2PatchesCreator(updatableContainer, intensityPatternPixelsPerUnit));
            return new ESurfacePatchProvider(patchesCreatorProxy, patchStamperOverseerFinalizer, commonExecutorUtProxy, mipmapExtractor, mipmapLevelToExtract);
        }

        public static GRing2PatchesCreator CreateRing2PatchesCreator(UltraUpdatableContainer updatableContainer, Dictionary<int, float> intensityPatternPixelsPerUnit )
        {
            TextureConcieverUTProxy conciever = new TextureConcieverUTProxy();
            updatableContainer.Add(conciever);

            Ring2RandomFieldFigureGenerator figureGenerator = new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(1, 1)
                });
            var utFigureGenerator = new RandomFieldFigureGeneratorUTProxy(figureGenerator);
            updatableContainer.Add(utFigureGenerator);

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(utFigureGenerator,
                new Ring2RandomFieldFigureRepositoryConfiguration(2, new Vector2(20, 20)));

            Quadtree<Ring2Region> regionsTree = Ring2TestUtils.CreateRegionsTreeWithPath3(randomFieldFigureRepository);

            return new GRing2PatchesCreator(
                new MonoliticRing2RegionsDatabase(regionsTree),
                new GRing2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever),
                new GRing2Deviser(),
                new Ring2PatchesOverseerConfiguration()
                {
                    IntensityPatternPixelsPerUnit = intensityPatternPixelsPerUnit
                }
            );
        }
    }
}
