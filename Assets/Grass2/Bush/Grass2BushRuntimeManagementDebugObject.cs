using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass2.Growing;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Repositioning;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.Management;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Grass2.Bush
{
    public class Grass2BushRuntimeManagementDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        private VegetationRuntimeManagement _runtimeManagement;
        private VegetationRuntimeManagementProxy _vegetationRuntimeManagementProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            ///// GRASSING!!!!!

            var singleGenerationArea = new Vector2(10, 10);
            var positionsProvider =
                new GrassVegetationSubjectsPositionsGenerator(
                    new GrassVegetationSubjectsPositionsGenerator.
                        GrassVegetationSubjectsPositionsGeneratorConfiguration()
                        {
                            PositionsGridSize = singleGenerationArea
                        });

            _debugGrassGroupsGrowerUnderTest = new DebugGrassGroupsGrowerUnderTest(new DebugBushPlanterUnderTest());
            _debugGrassGroupsGrowerUnderTest.Start(ComputeShaderContainer);

            GrassGroupsGrower grassGroupsGrower = _debugGrassGroupsGrowerUnderTest.Grower;
            Grass2RuntimeManager grass2RuntimeManager = new Grass2RuntimeManager(grassGroupsGrower,
                new Grass2RuntimeManager.Grass2RuntimeManagerConfiguration()
                {
                    GroupSize = singleGenerationArea
                });
            var vegetationSubjectsChangesListener =
                new Grass2RuntimeManagerProxy(grass2RuntimeManager);

            //////

            _runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: positionsProvider,
                vegetationSubjectsChangesListener: vegetationSubjectsChangesListener,
                visibleEntitiesContainer: new VegetationSubjectsVisibleEntitiesContainer(),
                configuration: new VegetationRuntimeManagementConfiguration()
                {
                    DetailFieldsTemplate = new SingleSquareDetailFieldsTemplate(100, VegetationDetailLevel.FULL),
                    UpdateMinDistance = 10
                });

            _vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(_runtimeManagement);
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
                _vegetationRuntimeManagementProxy.SynchronicUpdate(newPosition);
                const int maxMsPerFrame = 4000;

                //_globalInstancingContainer.FinishUpdateBatch();
            }
            _debugGrassGroupsGrowerUnderTest.Update();
        }
    }
}