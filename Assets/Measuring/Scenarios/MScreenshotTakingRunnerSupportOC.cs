using System.Collections.Generic;
using System.IO;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Measuring.Scenarios
{
    public class MScreenshotTakingRunnerSupportOC : MonoBehaviour, IMRunnerSupport
    {
        public int TakeScreenshotEveryNthFrame = -1;
        public int MeasurementIndexToTakeScreenshotIn = 1;
        private MTestingRunnerGO _testingRunner;
        private string _measurementsPath;
        private bool _requestedScreenshot = false;
        private int _requestedScreenshotFrame = 0;

        public void MyStart(LineMeasuringPpModule lineMeasuringModule)
        {
            _testingRunner = GetComponent<MTestingRunnerGO>();
            _measurementsPath = _testingRunner.OneTestConfiguration.TestResultsDirectoryPath + "/screenshots/";

            Directory.CreateDirectory(_measurementsPath);
        }

        public bool MyOnRenderImage(RenderTexture src, RenderTexture dest)
        {
             return false;
        }

        public void MyLateOnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_requestedScreenshot && (_requestedScreenshotFrame == Time.frameCount))
            {
                var artisticRenderTex = _testingRunner.RenderTargets.ArtisticMainTexture;
                var artisticTex2D = UltraTextureRenderer.RenderTextureToTexture2D(artisticRenderTex);
                SavingFileManager.SaveTextureToPngFile($"{_measurementsPath}/ArtisticTex.{_testingRunner.RequestedTestFrame}.png", artisticTex2D);
                GameObject.Destroy(artisticTex2D);
                _requestedScreenshot = false;
            }
        }

        public void MyOnMeasurementsMade(List<IMeasurementResult> measurementResults, MeasurementScreenshotsSet set)
        {
            //TODO Once Code from MyOnUpdate was here and Check at line 33 was < not ==. In that setur screenshots could be taken only if there was measurement. 
            // So no measurements were taken with only performance analisys animation playing
        }

        public void MyOnUpdate()
        {
            if (_testingRunner.RequestedTestFrame == MeasurementIndexToTakeScreenshotIn)
            {
                _requestedScreenshot = true;
                _requestedScreenshotFrame = Time.frameCount;
            }

            if (TakeScreenshotEveryNthFrame > 0)
            {
                if ((_testingRunner.RequestedTestFrame % TakeScreenshotEveryNthFrame) == 0)
                {
                    _requestedScreenshot = true;
                    _requestedScreenshotFrame = Time.frameCount;
                }
            }
        }

        public List<GaugeType> RequiredGauges()
        {
            return new List<GaugeType>();
        }
    }
}