using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Grass;
using Assets.Grass2.Billboards;
using Assets.Grass2.Groups;
using Assets.Grass2.IntensitySampling;
using Assets.Grass2.Planting;
using Assets.Grass2.PositionResolving;
using Assets.Grass2.Types;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Grass2.Bush
{
    public class GrassBushPlantingManagerDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;
        private DebugBushPlanterUnderTest _debugGrassPlanterUnderTest;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            _debugGrassPlanterUnderTest = new DebugBushPlanterUnderTest();
            _debugGrassPlanterUnderTest.Start(ComputeShaderContainer);
            var grassGroupsPlanter = _debugGrassPlanterUnderTest.GrassGroupsPlanter;

            var generationArea = new MyRectangle(0, 0, 90, 90);

            var randomFigureGenerator = new RandomFieldFigureGeneratorUTProxy(
                new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                    new Ring2RandomFieldFigureGeneratorConfiguration()
                    {
                        PixelsPerUnit = new Vector2(1, 1)
                    }));
            var randomFigure = randomFigureGenerator.GenerateRandomFieldFigureAsync(
                RandomFieldNature.FractalSimpleValueNoise3, 312,
                new MyRectangle(0, 0, 30, 30)).Result;

            var intensityFigureProvider = new IntensityFromRandomFiguresCompositionProvider(
                PoissonDiskSamplingDebugObject.CreateDebugRandomFieldFigure(),
                randomFigure, 0.3f);

            grassGroupsPlanter.AddGrassGroup(generationArea, GrassType.Debug1, intensityFigureProvider);

            _debugGrassPlanterUnderTest.FinalizeStart();
            CreateDebugIntensityTexture(intensityFigureProvider);
        }

        public void CreateDebugIntensityTexture(IIntensitySamplingProvider provider)
        {
            var tex = new Texture2D(16, 16, TextureFormat.ARGB32, false, true);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    tex.SetPixel(x, y, new Color(provider.Sample(new Vector2(x / 15f, y / 15f)), 0, 0, 1));
                }
            }
            tex.Apply(false);
            SavingFileManager.SaveTextureToPngFile($@"C:\inz2\Intensity.png", tex);
        }

        public void Update()
        {
            _debugGrassPlanterUnderTest.Update();
        }
    }

    public class DebugBushPlanterUnderTest : IDebugPlanterUnderTest
    {
        private GrassGroupsPlanter _grassGroupsPlanter;
        private GlobalGpuInstancingContainer _globalGpuInstancingContainer;
        private DesignBodySpotUpdaterProxy _designBodySpotUpdaterProxy;

        public void Start(ComputeShaderContainerGameObject computeShaderContainer)
        {
            var commonExecutor = new CommonExecutorUTProxy();
            var shaderExecutorObject = new UnityThreadComputeShaderExecutorObject();

            var updater =
                new DesignBodySpotUpdater(new DesignBodySpotChangeCalculator(computeShaderContainer,
                    shaderExecutorObject, commonExecutor, HeightDenormalizer.Identity));

            _designBodySpotUpdaterProxy = new DesignBodySpotUpdaterProxy(updater);
            updater.SetChangesListener(new LambdaSpotPositionChangesListener(null, dict =>
            {
                foreach (var pair in dict)
                {
                    _grassGroupsPlanter.GrassGroupSpotChanged(pair.Key, pair.Value);
                }
            }));
            _designBodySpotUpdaterProxy.StartThreading(() => { });


            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBillboardMesh(0, 1);

            var instancingMaterial = new Material(Shader.Find("Custom/Vegetation/GrassBushBillboard.Instanced"));
            instancingMaterial.enableInstancing = true;

            /// CLAN

            var billboardsFileManger = new Grass2BillboardClanFilesManager();
            var clan = billboardsFileManger.Load(@"C:\inz\billboards\", new IntVector2(256, 256));
            var singleToDuo = new Grass2BakingBillboardClanGenerator(computeShaderContainer, shaderExecutorObject);
            var bakedClan = singleToDuo.GenerateBakedAsync(clan).Result;
            /// 


            var commonUniforms = new UniformsPack();
            commonUniforms.SetUniform("_BendingStrength", 0.0f);
            commonUniforms.SetUniform("_WindDirection", Vector4.one);

            commonUniforms.SetTexture("_DetailTex", bakedClan.DetailTextureArray);
            commonUniforms.SetTexture("_BladeSeedTex", bakedClan.BladeSeedTextureArray);

            var instancingContainer = new GpuInstancingVegetationSubjectContainer(
                new GpuInstancerCommonData(mesh, instancingMaterial, commonUniforms),
                new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                {
                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_InitialBendingValue", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantBendingStiffness", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantDirection", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_RandSeed", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_ArrayTextureIndex", GpuInstancingUniformType.Float),
                })
            );

            _globalGpuInstancingContainer = new GlobalGpuInstancingContainer();
            var bucketId = _globalGpuInstancingContainer.CreateBucket(instancingContainer);
            GrassGroupsContainer grassGroupsContainer =
                new GrassGroupsContainer(_globalGpuInstancingContainer, bucketId);

            IGrassPositionResolver grassPositionResolver =
                new PoissonDiskSamplerPositionResolver(new MyRange(1.5f * 0.4f * 10, 10 * 2 * 1.3f));
            //IGrassPositionResolver grassPositionResolver = new SimpleRandomSamplerPositionResolver();

            GrassDetailInstancer grassDetailInstancer = new GrassDetailInstancer();


            _grassGroupsPlanter = new GrassGroupsPlanter(
                grassDetailInstancer, grassPositionResolver, grassGroupsContainer, _designBodySpotUpdaterProxy,
                new Grass2BushAspectsGenerator(bakedClan), //todo! 
                GrassDebugUtils.BushTemplates, Repositioner.Identity);
        }

        public void FinalizeStart()
        {
            _designBodySpotUpdaterProxy.UpdateBodiesSpots(GrassPlantingManagerDebugObject
                .GenerateDebugUpdatedTerrainTextures());
            _globalGpuInstancingContainer.StartThread();
            _designBodySpotUpdaterProxy.SynchronicUpdate();
            _globalGpuInstancingContainer.FinishUpdateBatch();
        }

        public void Update()
        {
            _globalGpuInstancingContainer.FinishUpdateBatch();
            _globalGpuInstancingContainer.DrawFrame();
            _designBodySpotUpdaterProxy.SynchronicUpdate();
        }

        public GrassGroupsPlanter GrassGroupsPlanter => _grassGroupsPlanter;
    }
}