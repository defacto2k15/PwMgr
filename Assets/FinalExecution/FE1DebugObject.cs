using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.ComputeShaders;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation;
using Assets.PreComputation.Configurations;
using Assets.Scheduling;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.FinalExecution
{
    public class FE1DebugObject : MonoBehaviour
    {
        public FeGRingConfiguration FeGRingConfiguration;
        public FinalVegetationConfiguration VegetationConfiguration;
        public ComputeShaderContainerGameObject ContainerGameObject;
        public Camera ActiveCamera;
        public GraphicsOverlay GraphicsOverlay;
        public bool UseMultithreading;
        public TerrainShapeDbConfiguration TerrainShapeDbConfiguration;

        private UltraUpdatableContainer _ultraUpdatableContainer;
        private GameInitializationFields _gameInitializationFields = new GameInitializationFields();
        private FEConfiguration _configuration;
        private MyStopWatch msw = new MyStopWatch();

        // Use this for initialization
        void Start()
        {
            _configuration = new FEConfiguration(new FilePathsConfiguration()) {Multithreading = UseMultithreading};
            _configuration.TerrainShapeDbConfiguration = TerrainShapeDbConfiguration;
            FeGRingConfiguration.FeConfiguration = _configuration;
            VegetationConfiguration.FeConfiguration = _configuration;

            TaskUtils.SetGlobalMultithreading(_configuration.Multithreading);
            TaskUtils.ExecuteActionWithOverridenMultithreading(true, () =>
            {
                MyProfiler.BeginSample("Sample1");
                msw.StartSegment("Game initialization");

                GlobalServicesProfileInfo servicesProfileInfo = new GlobalServicesProfileInfo();
                if (GraphicsOverlay != null)
                {
                    GraphicsOverlay.ServicesProfileInfo = servicesProfileInfo;
                }

                _ultraUpdatableContainer = new UltraUpdatableContainer(
                    _configuration.SchedulerConfiguration,
                    servicesProfileInfo,
                    _configuration.UpdatableContainerConfiguration);

                _configuration.TerrainShapeDbConfiguration.UseTextureSavingToDisk = true;

                _gameInitializationFields.SetField(ContainerGameObject);
                _gameInitializationFields.SetField(_configuration.Repositioner);
                _gameInitializationFields.SetField(_configuration.HeightDenormalizer);

                var initializingHelper =
                    new FEInitializingHelper(_gameInitializationFields, _ultraUpdatableContainer, _configuration);
                initializingHelper.InitializeUTService(new TextureConcieverUTProxy());
                initializingHelper.InitializeUTService(new UnityThreadComputeShaderExecutorObject(_configuration.UseMultistepComputeShaderExecution));
                initializingHelper.InitializeUTService(new CommonExecutorUTProxy());
                initializingHelper.CreatePathProximityTextureDb();

                SetInitialCameraPosition();

                initializingHelper.InitializeDesignBodySpotUpdater();
                initializingHelper.InitializeUTRendererProxy();
                initializingHelper.InitializeUTService(new MeshGeneratorUTProxy(new MeshGeneratorService()));
                initializingHelper.InitializeMonoliticRing2RegionsDatabase();

                var finalSurfacePathInitialization =
                    new Ring2PatchInitializer(_gameInitializationFields, _ultraUpdatableContainer, new Ring2PatchInitializerConfiguration(_configuration));
                finalSurfacePathInitialization.Start();

                var finalTerrainInitialization =
                    new FinalTerrainInitialization(_ultraUpdatableContainer, _gameInitializationFields, _configuration, FeGRingConfiguration);
                finalTerrainInitialization.Start();

                initializingHelper.InitializeGlobalInstancingContainer();
                var finalVegetation = new FinalVegetation(_gameInitializationFields, _ultraUpdatableContainer, VegetationConfiguration);
                finalVegetation.Start();
            });

            MyProfiler.EndSample();
        }

        UpdaterUntilException _updaterUntilException = new UpdaterUntilException();

        public void Update()
        {
            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(new EncapsulatedCameraForUpdate(ActiveCamera)); });
            if (msw != null)
            {
                Debug.Log("T65: game initialization took: " + msw.CollectResults());
                msw = null;
            }
        }

        private void SetInitialCameraPosition()
        {
            if (!_configuration.UseCameraFlightDemo)
            {
                ActiveCamera.transform.position = _configuration.CameraStartPosition;
            }
        }

        public void DebugMoveCamera()
        {
            ActiveCamera.transform.localPosition += new Vector3(0,0,6);
        }

        public void OnApplicationQuit()
        {
            if (_ultraUpdatableContainer != null)
            {
                _ultraUpdatableContainer.Stop();
            }
        }
    }
}