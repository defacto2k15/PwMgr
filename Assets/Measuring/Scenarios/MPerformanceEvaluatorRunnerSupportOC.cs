using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Measuring.Scenarios
{
    public class MPerformanceEvaluatorRunnerSupportOC :  MonoBehaviour, IMRunnerSupport
    {
        private MTestingRunnerGO _testingRunner;
        private string _measurementsPath;
        private int _thisAnimationRepeatCycleIndex;
        private List<FrameNumberAndDurationPair> _framesList;
        private bool _resultsWrittenToFile;

        public void MyStart(LineMeasuringPpModule lineMeasuringModule)
        {
            _testingRunner = GetComponent<MTestingRunnerGO>();
            _measurementsPath = _testingRunner.OneTestConfiguration.TestResultsDirectoryPath + "/performanceEvaluation/";
            Directory.CreateDirectory(_measurementsPath);
            _framesList = new List<FrameNumberAndDurationPair>();
            _resultsWrittenToFile = false;
        }

        public bool MyOnRenderImage(RenderTexture src, RenderTexture dest)
        {
            return false;
        }

        public void MyLateOnRenderImage(RenderTexture src, RenderTexture dest)
        {
        }

        public void MyOnMeasurementsMade(List<IMeasurementResult> measurementResults, MeasurementScreenshotsSet set)
        {
        }

        public void MyOnUpdate()
        {
            _framesList.Add(new FrameNumberAndDurationPair(){Duration = Time.unscaledDeltaTime, FrameNo = _testingRunner.RequestedTestFrame});

            var lastTestFrame = _testingRunner.OneTestConfiguration.FirstTestFrame + _testingRunner.OneTestConfiguration.TestFramesCount;
            if ( _testingRunner.RequestedTestFrame+1  < lastTestFrame )
            {
                    _testingRunner.RequestedTestFrame++;
            }
            else if (lastTestFrame == _testingRunner.RequestedTestFrame+1)
            {
                if (_thisAnimationRepeatCycleIndex >= _testingRunner.OneTestConfiguration.PerformanceEvaluatorTimesToRepeatAnimation - 1)
                {
                    Debug.Log("MPerformanceEvaluatorRunnerSupportOC - writingDurationsToFile");
                    WriteFrameDurationsToFile();
                    _testingRunner.RequestedTestFrame++;
                }
                else
                {
                    _thisAnimationRepeatCycleIndex++;
                    _testingRunner.RequestedTestFrame = _testingRunner.OneTestConfiguration.FirstTestFrame;
                }
            }
        }

        private void WriteFrameDurationsToFile()
        {
            File.WriteAllText($"{_measurementsPath}/FrameDurations.json",
                JsonUtility.ToJson(new FrameNumberAndDurationList(){Durations = _framesList}));
        }

        public List<GaugeType> RequiredGauges()
        {
            return new List<GaugeType>();
        }
    }

    [Serializable]
    public class FrameNumberAndDurationPair
    {
        public int FrameNo;
        public float Duration;
    }

    [Serializable]
    public class FrameNumberAndDurationList
    {
        public List<FrameNumberAndDurationPair> Durations;
    }
}
