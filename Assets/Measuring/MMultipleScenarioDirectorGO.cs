using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Measuring.Scenarios;
using Assets.Utils;
using Assets.Utils.CameraUtils;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Assets.Measuring
{
    public class MMultipleScenarioDirectorGO : MonoBehaviour
    {
        public GameObject InitialCameraPositionMarker;
        public string TestingRootPath;
        public GameObject CameraPrefab;
        public GameObject HatchedObject;
        public PlayableDirector CameraDirector;
        public PlayableDirector LightDirector;
        public Light PointLight;
        public MMultipleTestConfiguration MultipleConfiguration;
        private Queue<MOneTestConfiguration> _configurations;
        private MOneScenarioDirectorGO _currentScenarioDirector;
        private int _fullTestsCount;

        public void Start()
        {
            if (!Application.isEditor)
            {
                TestingRootPath = Application.dataPath + "/../MultipleTests/";
            }
            List<MOneTestConfiguration> singleTestsConfigurations = GenerateSingleConfigurations();
            _configurations = new Queue<MOneTestConfiguration>(singleTestsConfigurations);
            _fullTestsCount = _configurations.Count;
            StartNextTest();
        }

        public void Update()
        {
            if (AnyTestActive())
            {
                if (CurrentTestFinished())
                {
                    if (AnyMoreTests())
                    {
                        FinalizeCurrentTest();
                        StartNextTest();
                    }
                    else
                    {
                        Debug.Log("M632 Testing ended");
                        _currentScenarioDirector = null;
                    }
                }
            }

        }

        private void FinalizeCurrentTest()
        {
            GameObject.Destroy(FindObjectOfType<Camera>().gameObject);
        }

        private bool AnyMoreTests()
        {
            return _configurations.Any();
        }

        private void StartNextTest()
        {
            MOneTestConfiguration nextTestConfiguration = GetNextTest();
            _currentScenarioDirector = StartTest(nextTestConfiguration);
            Debug.Log($"M512 Starting test {(_fullTestsCount)-_configurations.Count}/{_fullTestsCount} : {nextTestConfiguration.TestName}");
        }

        private MOneTestConfiguration GetNextTest()
        {
            return _configurations.Dequeue();
        }

        private bool CurrentTestFinished()
        {
            return _currentScenarioDirector.TestFinished;
        }

        private bool AnyTestActive()
        {
            return _currentScenarioDirector != null;
        }

        private MOneScenarioDirectorGO StartTest(MOneTestConfiguration configuration)
        {
            var cameraObject = GameObject.Instantiate(CameraPrefab);
            cameraObject.transform.position = InitialCameraPositionMarker.transform.position;
            var scenarioDirector = cameraObject.GetComponent<MOneScenarioDirectorGO>();
            scenarioDirector.HatchedObject = HatchedObject;
            scenarioDirector.Configuration = configuration;
            cameraObject.GetComponent<CameraMouseRotatorByAnglesOC>().Target = HatchedObject.transform;

            var testingRunner = cameraObject.GetComponent<MTestingRunnerGO>();
            testingRunner.CameraDirector = CameraDirector;
            testingRunner.LightDirector = LightDirector;
            testingRunner.PointLightObject = PointLight;
            return scenarioDirector;
        }

        private List<MOneTestConfiguration> GenerateSingleConfigurations()
        {
            List<MPremadeTestTemplate> premadeTemplates;
            if (MultipleConfiguration.UsePremadeTemplates)
            {
                premadeTemplates = MultipleConfiguration.PremadeTemplates.Where(c => c.Enabled).ToList();
            }
            else
            {
                premadeTemplates = new List<MPremadeTestTemplate>()
                {
                    new MPremadeTestTemplate()
                    {
                        Enabled = true,
                        Mesh = MultipleConfiguration.TemplateOneTestConfiguration.MeshToUse,
                        Mode = MultipleConfiguration.TemplateOneTestConfiguration.GeneratingMode,
                        TimelineAsset = MultipleConfiguration.TemplateOneTestConfiguration.CameraTimelineAsset,
                        LightTimelineAsset= MultipleConfiguration.TemplateOneTestConfiguration.LightTimelineAsset,
                    }
                };
            }

            var configurationsOutList = new List<MOneTestConfiguration>();
            foreach(var aPremadeTemplate in premadeTemplates)
            {
                var meshesToTestList = MultipleConfiguration.MeshesToTest.ToList();
                if (!meshesToTestList.Any())
                {
                    meshesToTestList.Add(aPremadeTemplate.Mesh);
                }

                var modesToTestList = MultipleConfiguration.ModesToTest.ToList();
                if (!modesToTestList.Any())
                {
                    modesToTestList.Add(aPremadeTemplate.Mode);
                }

                var timelineAssetsToTestList = MultipleConfiguration.TimelineAssetsToTest.ToList();
                if (!timelineAssetsToTestList.Any())
                {
                    timelineAssetsToTestList.Add(aPremadeTemplate.TimelineAsset);
                }

                foreach (var aMesh in meshesToTestList)
                {
                    foreach (var aMode in modesToTestList)
                    {
                        foreach (var aTimeline in timelineAssetsToTestList)
                        {
                            var testName = $"{aMode}_{aTimeline.name}_{aMesh.MeshBufferName}";
                            var newOneConf = MultipleConfiguration.TemplateOneTestConfiguration.MyClone();

                            newOneConf.MeshToUse = aMesh;
                            newOneConf.GeneratingMode = aMode;
                            newOneConf.CameraTimelineAsset = aTimeline;
                            newOneConf.TestName = testName;
                            newOneConf.TestResultsDirectoryPath = TestingRootPath + testName + "/";
                            newOneConf.LightTimelineAsset = aPremadeTemplate.LightTimelineAsset;
                            configurationsOutList.Add(newOneConf);
                        }
                    }
                }
            }
            return configurationsOutList;

        }

    }

    [Serializable]
    public class MMultipleTestConfiguration
    {
        public MOneTestConfiguration TemplateOneTestConfiguration;
        public List<MeshSpecification> MeshesToTest;
        public List<HatchGeneratingMode> ModesToTest;
        public List<TimelineAsset> TimelineAssetsToTest;
        public bool UsePremadeTemplates;
        public List<MPremadeTestTemplate> PremadeTemplates;
    }

    [Serializable]
    public class MPremadeTestTemplate
    {
        public MeshSpecification Mesh;
        public HatchGeneratingMode Mode;
        public TimelineAsset TimelineAsset;
        public TimelineAsset LightTimelineAsset;
        public bool Enabled;
    }
}