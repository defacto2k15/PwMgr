using System.Collections.Generic;
using Assets.Measuring.Gauges;
using Assets.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Measuring.Scenarios
{

    public class MImageTestingRunnerSupportOC : MonoBehaviour, IMRunnerSupport
    {
        public uint IdOfLineToDraw;
        public Material MTestImageRenderingMat;

        private MTestingRunnerGO _testingRunner;
        private MSampledLineRenderer _lineRenderer;
        private LinesLayoutResult _lastLinesLayoutResult;
        private uint[,] _lastIdsArray;

        public void MyStart(LineMeasuringPpModule lineMeasuringModule)
        {
            _testingRunner = GetComponent<MTestingRunnerGO>();
            _lineRenderer = new MSampledLineRenderer(FindObjectOfType<Camera>());

            MTestImageRenderingMat.SetTexture("_ArtisticMainRenderTex", lineMeasuringModule.RenderTargets.ArtisticMainTexture);
            MTestImageRenderingMat.SetTexture("_HatchMainRenderTex", lineMeasuringModule.RenderTargets.HatchMainTexture);
            MTestImageRenderingMat.SetTexture("_IdRenderTex", lineMeasuringModule.RenderTargets.HatchIdTexture);
            MTestImageRenderingMat.SetTexture("_WorldPos1RenderTex", lineMeasuringModule.RenderTargets.WorldPosition1Texture);
            MTestImageRenderingMat.SetTexture("_WorldPos2RenderTex", lineMeasuringModule.RenderTargets.WorldPosition2Texture);
        }

        public bool MyOnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, MTestImageRenderingMat);
            return true;
        }

        public void MyLateOnRenderImage(RenderTexture src, RenderTexture dest)
        {
        }

        public void MyOnMeasurementsMade(List<IMeasurementResult> measurementResults, MeasurementScreenshotsSet set)
        {
            _lastIdsArray = set.IdArray;

            foreach (var aResult in measurementResults)
            {
                var illustration = aResult.GenerateIllustration();
                var textureName = "_" + aResult.GetResultName() + "IllustrationTex";
                MTestImageRenderingMat.SetTexture(textureName, illustration);

                if (aResult is LinesLayoutResult result)
                {
                    _lastLinesLayoutResult = result;
                }
            }
        }

        public void MyOnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                Debug.Log("Wyświetlanie linii o id "+IdOfLineToDraw);
                if (_lastLinesLayoutResult != null)
                {
                    _lineRenderer.RenderLine(_lastLinesLayoutResult, IdOfLineToDraw);
                }
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                //Debug.Log("Wyświetlanie punktów należących do wszystkich linii");
                //_lineRenderer.RenderAllHatchesPixels(_lastLinesLayoutResult);
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log("Wyświetlanie punktów należących do linii o id "+IdOfLineToDraw);
                if (_lastLinesLayoutResult != null)
                {
                    _lineRenderer.RenderHatchPixels(_lastLinesLayoutResult, IdOfLineToDraw);
                }
            }

            if (Input.GetMouseButtonDown(2))
            {
                var mousePosition = new IntVector2(Mathf.RoundToInt(Input.mousePosition.x), Mathf.RoundToInt(Input.mousePosition.y));
                if (_lastIdsArray != null)
                {
                    var id = _lastIdsArray[mousePosition.X, mousePosition.Y];
                    Debug.Log("Id pod kursorem: "+id);
                }
            }


            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_testingRunner.InteractionMode == MTestingRunnerGO.MTestInteractionMode.Animation)
                {
                    Debug.Log("Changing interaction mode to free floating");
                    GetComponent<ExtendedFlycam>().enabled = true;
                    _testingRunner.InteractionMode = MTestingRunnerGO.MTestInteractionMode.FreeFloating;
                }
                else
                {
                    Debug.Log("Changing interaction mode to animation");
                    _testingRunner.InteractionMode = MTestingRunnerGO.MTestInteractionMode.Animation;
                    GetComponent<ExtendedFlycam>().enabled = false;
                }
            }

            if (_testingRunner.InteractionMode == MTestingRunnerGO.MTestInteractionMode.Animation)
            {
                if (Input.GetKeyDown(KeyCode.Comma) || Input.GetKey(KeyCode.Alpha9))
                {
                    _testingRunner.RequestedTestFrame = Mathf.Max(_testingRunner.RequestedTestFrame - 1, 0);
                }

                if (Input.GetKeyDown(KeyCode.Period) || Input.GetKey(KeyCode.Alpha0))
                {
                    _testingRunner.RequestedTestFrame = Mathf.Min(_testingRunner.RequestedTestFrame + 1, _testingRunner.OneTestConfiguration.FirstTestFrame+_testingRunner.OneTestConfiguration.TestFramesCount);
                }
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("Forcing measurement");
                _testingRunner.ForceMeasurement();
            }

            _lineRenderer.Update();
        }

        public List<GaugeType> RequiredGauges()
        {
            return new List<GaugeType>()
            {
                GaugeType.BlockSpecificationGauge, GaugeType.LinesLayoutGauge, GaugeType.LinesWidthGauge, GaugeType.StrokesPixelCountGauge
            };
        }
    }

    public interface  IMRunnerSupport
    {
        void MyStart(LineMeasuringPpModule lineMeasuringModule);
        bool MyOnRenderImage(RenderTexture src, RenderTexture dest);
        void MyLateOnRenderImage(RenderTexture src, RenderTexture dest);
        void MyOnMeasurementsMade(List<IMeasurementResult> measurementResults, MeasurementScreenshotsSet set);
        void MyOnUpdate();
        List<GaugeType> RequiredGauges();
    }
}