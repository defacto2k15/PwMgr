using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Measuring.Scenarios
{
    public class MMeasurementRunnerSupportOC : MonoBehaviour, IMRunnerSupport
    {
        public float TimeToWaitBetweenTests = 1;
        public int FramesToWaitBeforeTesting = 5;
        public bool TestOneFrame;
        private MTestingRunnerGO _testingRunner;
        private int _framesTestedCount = 0;
        private string _measurementsPath;
        private float _lastTestTime;

        public void MyStart(LineMeasuringPpModule lineMeasuringModule)
        {
            _testingRunner = GetComponent<MTestingRunnerGO>();
            _measurementsPath = _testingRunner.OneTestConfiguration.TestResultsDirectoryPath + "/measurements/";

            Directory.CreateDirectory(_measurementsPath);
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
            foreach (var aResult in measurementResults)
            {
                File.WriteAllText($"{_measurementsPath}/{aResult.GetResultName()}.{_testingRunner.RequestedTestFrame}.csv", aResult.ToCsvString());
            }

            _lastTestTime = Time.time;
        }

        public void MyOnUpdate()
        {
            if (Time.frameCount > FramesToWaitBeforeTesting)
            {
                if (TestOneFrame)
                {
                    if (_framesTestedCount == 0)
                    {
                        _testingRunner.ForceMeasurement();
                    }
                }
                else
                {
                    var lastTestFrame = _testingRunner.OneTestConfiguration.FirstTestFrame + _testingRunner.OneTestConfiguration.TestFramesCount;
                    if (lastTestFrame > _testingRunner.RequestedTestFrame)
                    {
                        if (Time.time - _lastTestTime > TimeToWaitBetweenTests)
                        {
                            _testingRunner.ForceMeasurement();
                            _framesTestedCount++;
                            _testingRunner.RequestedTestFrame++;
                        }
                    }
                }
            }
        }

        public List<GaugeType> RequiredGauges()
        {
            return new List<GaugeType>()
            {
                GaugeType.BlockSpecificationGauge, GaugeType.LinesLayoutGauge, GaugeType.LinesWidthGauge, GaugeType.StrokesPixelCountGauge
            };
        }
    }
}