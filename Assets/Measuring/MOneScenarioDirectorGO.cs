using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring.Scenarios;
using Assets.NPR.Filling;
using UnityEngine;
using UnityEngine.Timeline;
using MScreenshotTakingRunnerSupportOC = Assets.Measuring.Scenarios.MScreenshotTakingRunnerSupportOC;
using Object = UnityEngine.Object;

namespace Assets.Measuring
{
    public class MOneScenarioDirectorGO : MonoBehaviour
    {
        public GameObject HatchedObject;
        public MOneTestConfiguration Configuration;
        private IMRunner _runner;

        public void Start()
        {
            var cameraObject = gameObject;
            RemoveAdditionalComponentsFromHatchedObject();
            HatchedObject.GetComponent<MeshFilter>().mesh = Configuration.MeshToUse.MeshToUse;
            HatchedObject.transform.localScale = Configuration.MeshToUse.Scale;
            var configurer = FindObjectOfType<HatchGeneratingEnviromentConfigurerOC>();
            var ppDirector = configurer.ConfigureEnviroment(Configuration.GeneratingMode, cameraObject, HatchedObject);

            HatchedObject.GetComponents<IOneTestConfigurationConsumer>()
                .ToList()
                .ForEach(c => c.ConsumeConfiguration(Configuration));

            ppDirector?.SetAutonomicRendering(false);

            _runner = StartRunner<MTestingRunnerGO>(ppDirector);
        }

        private void RemoveAdditionalComponentsFromHatchedObject()
        {
            var componentsToRemove = HatchedObject.GetComponents<Component>()
                .Where(c => !((c is MeshFilter) || (c is MeshRenderer) || (c is Transform))).ToList();
            componentsToRemove.ForEach(c => GameObject.Destroy(c));
        }

        private IMRunner StartRunner<T>(INprRenderingPostProcessingDirector ppDirector) where T : IMRunner
        {
            IMRunner runner = GetComponent<T>();

            var runnerSupports = new List<IMRunnerSupport>();
            if (Configuration.UseImageTestingRunnerSupport)
            {
                runnerSupports.Add(GetComponent<MImageTestingRunnerSupportOC>());
            }
            if (Configuration.UseMeasurementRunnerSupport)
            {
                runnerSupports.Add(GetComponent<MMeasurementRunnerSupportOC>());
            }
            if (Configuration.UsePerformanceEvaluatorRunnerSupportOC)
            {
                runnerSupports.Add(GetComponent<MPerformanceEvaluatorRunnerSupportOC>());
            }

            if (Configuration.UseScreenshotTakingRunnerSupport)
            {
                var mScreenshotTakingRunnerSupportOc = GetComponent<MScreenshotTakingRunnerSupportOC>();
                mScreenshotTakingRunnerSupportOc.TakeScreenshotEveryNthFrame= Configuration.TakeScreenshotEveryNthFrame;
                runnerSupports.Add(mScreenshotTakingRunnerSupportOc);
            }

            runner.Enable(Configuration, ppDirector, runnerSupports);
            return runner;
        }

        public bool TestFinished => _runner.TestFinished;
    }

    public enum HatchGeneratingMode
    {
        Tam, NoTemporalCoherency, ShowerDoor,
        Breslav, Wolowski, Jordane, Szecsi, TamIss, MMStandard, MMGeometric
    }


    [Serializable]
    public class MOneTestConfiguration 
    {
        public MeshSpecification MeshToUse;
        public HatchGeneratingMode GeneratingMode;
        public TimelineAsset CameraTimelineAsset;
        public TimelineAsset LightTimelineAsset;
        public string TestResultsDirectoryPath;
        public string TestName;
        public int SequenceFramesCount;
        public int FirstTestFrame;
        public int TestFramesCount;
        public int PerformanceEvaluatorTimesToRepeatAnimation;

        public bool UseImageTestingRunnerSupport;
        public bool UseMeasurementRunnerSupport;
        public bool UseScreenshotTakingRunnerSupport;
        public bool UsePerformanceEvaluatorRunnerSupportOC;
        public int TakeScreenshotEveryNthFrame = -1;

        public MOneTestConfiguration MyClone()
        {
            return new MOneTestConfiguration()
            {
                CameraTimelineAsset = CameraTimelineAsset,
                MeshToUse = MeshToUse,
                UseImageTestingRunnerSupport = UseImageTestingRunnerSupport,
                GeneratingMode = GeneratingMode,
                TestName = TestName,
                TestResultsDirectoryPath = TestResultsDirectoryPath,
                UseMeasurementRunnerSupport = UseMeasurementRunnerSupport,
                UseScreenshotTakingRunnerSupport = UseScreenshotTakingRunnerSupport,
                SequenceFramesCount = SequenceFramesCount,
                FirstTestFrame = FirstTestFrame,
                TestFramesCount = TestFramesCount,
                TakeScreenshotEveryNthFrame = TakeScreenshotEveryNthFrame,
                LightTimelineAsset = LightTimelineAsset,
                UsePerformanceEvaluatorRunnerSupportOC = UsePerformanceEvaluatorRunnerSupportOC,
                PerformanceEvaluatorTimesToRepeatAnimation = PerformanceEvaluatorTimesToRepeatAnimation
            };
        }
    }

    [Serializable]
    public class MeshSpecification
    {
        public Mesh MeshToUse;
        public string MeshBufferName;
        public Vector3 Scale;
    }
}
