using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation.Configurations;
using Assets.ShaderUtils;
using Assets.TerrainMat;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.RuntimeManagement.Management;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement
{
    public class VegetationRuntimeManagementWithSpotDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;
        public float HeightToChange;

        private VegetationRuntimeManagement _runtimeManagement;
        private VegetationRuntimeManagementProxy _vegetationRuntimeManagementProxy;
        private DummyVegetationSubjectInstanceContainer _dummyVegetationSubjectInstanceContainer;
        private GlobalGpuInstancingContainer _globalInstancingContainer;
        private ForgingVegetationSubjectInstanceContainerProxy _forgingContainerProxy;

        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private CommonExecutorUTProxy _commonExecutor;
        private DesignBodySpotUpdaterProxy _designBodySpotUpdaterProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            TreePrefabManager prefabManager = new TreePrefabManager();
            TreeClan clan = prefabManager.LoadTreeClan("beech");
            _commonExecutor = new CommonExecutorUTProxy();
            _shaderExecutorObject = new UnityThreadComputeShaderExecutorObject();

            _globalInstancingContainer = new GlobalGpuInstancingContainer();
            var representationContainer = new DesignBodyRepresentationContainer();
            DesignBodyInstanceBucketsContainer instanceBucketsContainer =
                new DesignBodyInstanceBucketsContainer(_globalInstancingContainer);

            var quadBillboardMesh = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>().mesh;
            var gShifter = new GTreeDetailProviderShifter(new DetailProviderRepository(), quadBillboardMesh);

            var filePathsConfiguration = new FilePathsConfiguration();
            var feConfiguration = new FEConfiguration(filePathsConfiguration);
            var finalVegetationConfiguration = new FinalVegetationConfiguration();
            finalVegetationConfiguration.FeConfiguration = feConfiguration;
            var treeFileManager = new TreeFileManager(new TreeFileManagerConfiguration()
                {
                    WritingTreeCompletedClanDirectory = finalVegetationConfiguration.TreeCompletedClanDirectiory
                });
            var combinationProvider = new GDesignBodyRepresentationInstanceCombinationProvider(treeFileManager, gShifter);

                var clanRepresentations = combinationProvider.CreateRepresentations(finalVegetationConfiguration.ShiftingConfigurations[VegetationSpeciesEnum.Beech], VegetationSpeciesEnum.Beech);
                representationContainer.InitializeLists(clanRepresentations);
                instanceBucketsContainer.InitializeLists(clanRepresentations);

            var updater =
                new DesignBodySpotUpdater(new DesignBodySpotChangeCalculator(ComputeShaderContainer,
                    _shaderExecutorObject, _commonExecutor, HeightDenormalizer.Identity));
            _designBodySpotUpdaterProxy = new DesignBodySpotUpdaterProxy(updater);

            _dummyVegetationSubjectInstanceContainer = new DummyVegetationSubjectInstanceContainer();
            _forgingContainerProxy = new ForgingVegetationSubjectInstanceContainerProxy(
                new ForgingVegetationSubjectInstanceContainer(
                    new DesignBodyPortrayalForger(
                        representationContainer,
                        instanceBucketsContainer),
                    _designBodySpotUpdaterProxy));
            updater.SetChangesListener(
                new LambdaSpotPositionChangesListener((dict) => _forgingContainerProxy.AddSpotModifications(dict)));

            _runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: CreateSamplePositionsDatabase(),
                vegetationSubjectsChangesListener: _forgingContainerProxy,
                visibleEntitiesContainer: new VegetationSubjectsVisibleEntitiesContainer(),
                configuration: new VegetationRuntimeManagementConfiguration()
                {
                    DetailFieldsTemplate = new CenterHolesDetailFieldsTemplate(new List<DetailFieldsTemplateOneLine>()
                    {
                        new DetailFieldsTemplateOneLine(VegetationDetailLevel.FULL, 0, 60),
                        new DetailFieldsTemplateOneLine(VegetationDetailLevel.REDUCED, 40, 120),
                        new DetailFieldsTemplateOneLine(VegetationDetailLevel.BILLBOARD, 100, 280),
                    }),
                    UpdateMinDistance = 10
                }
            );

            _vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(_runtimeManagement);
            _forgingContainerProxy.StartThreading(() => { });
            _designBodySpotUpdaterProxy.StartThreading(() => { });

            ChangeHeightTexture(0.5f);
        }


        private bool _once = false;

        public void Update()
        {
            _commonExecutor.Update();
            _shaderExecutorObject.Update();

            var newPosition = Camera.main.transform.position;
            if (!_once)
            {
                _once = true;
                _vegetationRuntimeManagementProxy.Start(newPosition);
                _vegetationRuntimeManagementProxy.StartThreading();
                _globalInstancingContainer.StartThread();
            }
            else
            {
                _designBodySpotUpdaterProxy.SynchronicUpdate();
                _globalInstancingContainer.DrawFrame();
                _vegetationRuntimeManagementProxy.AddUpdate(newPosition);
                const int maxMsPerFrame = 4000;
                _dummyVegetationSubjectInstanceContainer.Update(maxMsPerFrame);

                _globalInstancingContainer.FinishUpdateBatch();

                // synchro
                //_vegetationRuntimeManagementProxy.SynchronicUpdate(newPosition);
                //_unityThreadVegetationSubjectInstanceContainer.Update(999999);
            }
        }

        public void RecalculateHeight()
        {
            ChangeHeightTexture(HeightToChange);
        }

        private VegetationSubjectsPositionsDatabase CreateSamplePositionsDatabase()
        {
            var tree = new Quadtree<VegetationSubjectEntity>();
            for (int x = 0; x < 4000; x += 30)
            {
                for (int y = 0; y < 4000; y += 30)
                {
                    var newEntity = new VegetationSubjectEntity(
                        new DesignBodyLevel0Detail()
                        {
                            Pos2D = new Vector2(x, y),
                            Radius = 0,
                            Size = 1,
                            SpeciesEnum = VegetationSpeciesEnum.Beech
                        });
                    tree.Insert(MyNetTopologySuiteUtils.ToPointEnvelope(newEntity.Position2D), newEntity);
                }
            }
            return new VegetationSubjectsPositionsDatabase(tree);
        }

        private void ChangeHeightTexture(float height)
        {
            var heightArray = new float[12, 12];
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    heightArray[x, y] = height;
                }
            }
            var ha = new HeightmapArray(heightArray);
            var encodedHeightTex = HeightmapUtils.CreateTextureFromHeightmap(ha);

            var transformer = new TerrainTextureFormatTransformator(_commonExecutor);
            var plainHeightTex =
                transformer.EncodedHeightTextureToPlain(TextureWithSize.FromTex2D(encodedHeightTex));

            var normalArray = new Vector3[12, 12];
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    normalArray[x, y] = new Vector3(0.5f, 1.0f, 0.0f).normalized;
                }
            }
            var normalTexture = HeightmapUtils.CreateNormalTexture(normalArray);

            _designBodySpotUpdaterProxy.UpdateBodiesSpots(new UpdatedTerrainTextures()
            {
                HeightTexture = plainHeightTex,
                NormalTexture = normalTexture,
                TextureCoords = new MyRectangle(0, 0, 1, 1),
                TextureGlobalPosition = new MyRectangle(0, 0, 4000, 4000),
            });
        }
    }
}