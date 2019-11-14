using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Grass2.Growing;
using Assets.Grass2.Planting;
using Assets.Grass2.Types;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Repositioning;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding.Fitting;
using Assets.TerrainMat;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.Management;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Grass2
{ //todo przy szybkim ruszaniu kamerą są exceptiony, i to bez multithreadingu!!!
    public class Grass2RuntimeManagmentDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        private VegetationRuntimeManagement _runtimeManagement;
        private VegetationRuntimeManagementProxy _vegetationRuntimeManagementProxy;
        private GlobalGpuInstancingContainer _globalInstancingContainer;
        private ForgingVegetationSubjectInstanceContainerProxy _forgingContainerProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            TreePrefabManager prefabManager = new TreePrefabManager();
            TreeClan clan = prefabManager.LoadTreeClan("clan1");

            _globalInstancingContainer = new GlobalGpuInstancingContainer();
            var representationContainer = new DesignBodyRepresentationContainer();
            DesignBodyInstanceBucketsContainer instanceBucketsContainer =
                new DesignBodyInstanceBucketsContainer(_globalInstancingContainer);

            var quadBillboardMesh = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>().mesh;
            var shifter = new TreeClanToDetailProviderShifter(new DetailProviderRepository(), quadBillboardMesh,
                representationContainer, instanceBucketsContainer);
            shifter.AddClan(clan, VegetationSpeciesEnum.Tree1A);

            _forgingContainerProxy = new ForgingVegetationSubjectInstanceContainerProxy(
                new ForgingVegetationSubjectInstanceContainer(
                    new DesignBodyPortrayalForger(
                        representationContainer,
                        instanceBucketsContainer)));

            ///// GRASSING!!!!!

            var singleGenerationArea = new Vector2(10, 10);
            var positionsProvider = new CompositeVegetationSubjectsPositionProvider(
                new List<IVegetationSubjectsPositionsProvider>()
                {
                    //CreateSamplePositionsDatabase(),
                    new GrassVegetationSubjectsPositionsGenerator(
                        new GrassVegetationSubjectsPositionsGenerator.
                            GrassVegetationSubjectsPositionsGeneratorConfiguration()
                            {
                                PositionsGridSize = singleGenerationArea
                            })
                });

            _debugGrassGroupsGrowerUnderTest = new DebugGrassGroupsGrowerUnderTest(new DebugGrassPlanterUnderTest());
            _debugGrassGroupsGrowerUnderTest.Start(ComputeShaderContainer);

            GrassGroupsGrower grassGroupsGrower = _debugGrassGroupsGrowerUnderTest.Grower;
            Grass2RuntimeManager grass2RuntimeManager = new Grass2RuntimeManager(grassGroupsGrower,
                new Grass2RuntimeManager.Grass2RuntimeManagerConfiguration()
                {
                    GroupSize = singleGenerationArea
                });
            var vegetationSubjectsChangesListener = new CompositeVegetationSubjectsChangesListener(
                new List<VegetationSubjectsInstancingChangeListenerWithFilter>()
                {
                    new VegetationSubjectsInstancingChangeListenerWithFilter()
                    {
                        ChangeListener = new Grass2RuntimeManagerProxy(grass2RuntimeManager),
                        Filter = (entity => entity.Detail.SpeciesEnum == VegetationSpeciesEnum.Grass2SpotMarker)
                    },
                    //new VegetationSubjectsInstancingChangeListenerWithFilter()
                    //{
                    //    ChangeListener = _forgingContainerProxy,
                    //    Filter = entity => true
                    //}
                });

            //////

            _runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: positionsProvider,
                vegetationSubjectsChangesListener: vegetationSubjectsChangesListener,
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
                });

            _vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(_runtimeManagement);
            _forgingContainerProxy.StartThreading(() => { });
        }

        private bool _once = false;
        private DebugGrassGroupsGrowerUnderTest _debugGrassGroupsGrowerUnderTest;

        public void Update()
        {
            var newPosition = Camera.main.transform.position;
            if (!_once)
            {
                var msw = new MyStopWatch();
                msw.StartSegment("Starting segment.");
                _once = true;
                _vegetationRuntimeManagementProxy.Start(newPosition);
                _vegetationRuntimeManagementProxy.StartThreading();
                //_globalInstancingContainer.StartThread();
                _debugGrassGroupsGrowerUnderTest.FinalizeStart();
                Debug.Log("L8: segment " + msw.CollectResults());
            }
            else
            {
                //_globalInstancingContainer.DrawFrame();
                _vegetationRuntimeManagementProxy.AddUpdate(newPosition);
                const int maxMsPerFrame = 4000;

                //_globalInstancingContainer.FinishUpdateBatch();
            }
            _debugGrassGroupsGrowerUnderTest.Update();
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
                            Size = 0,
                            SpeciesEnum = VegetationSpeciesEnum.Tree1A
                        });
                    tree.Insert(MyNetTopologySuiteUtils.ToPointEnvelope(newEntity.Position2D), newEntity);
                }
            }
            return new VegetationSubjectsPositionsDatabase(tree);
        }
    }

    public class VegetationSubjectsInstancingChangeListenerWithFilter
    {
        public IVegetationSubjectInstancingContainerChangeListener ChangeListener;
        public Predicate<VegetationSubjectEntity> Filter;
    }
}