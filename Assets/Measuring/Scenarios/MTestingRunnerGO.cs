using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.Creator;
using Assets.NPR.Filling;
using Assets.Utils;
using Assets.Utils.CameraUtils;
using Assets.Utils.TextureRendering;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Assets.Measuring.Scenarios
{
    public class MTestingRunnerGO : MonoBehaviour, IMRunner
    {
        public Material SkeletonizerMaterial;
        public MOneTestConfiguration OneTestConfiguration;
        public PlayableDirector CameraDirector;
        public PlayableDirector LightDirector;
        public Light PointLightObject;

        public MTestInteractionMode InteractionMode { get; set; } = MTestInteractionMode.Animation;

        private LineMeasuringPpModule _lineMeasuringModule;
        private List<IGauge> _gauges;
        private INprRenderingPostProcessingDirector _auxDirector;
        private List<IMRunnerSupport> _runnerSupports;

        private int _lastMeasurementFrame = -1;
        private int _requestedTestFrame = -1;
        private bool _forceMeasurement = false;

        public void Start()
        {
            Physics.autoSimulation = false;
            _requestedTestFrame = OneTestConfiguration.FirstTestFrame;
            _lineMeasuringModule = new LineMeasuringPpModule();
            var cam = GetComponent<Camera>();
            _lineMeasuringModule.Initialize(cam);
            if (_auxDirector == null)
            {
                _lineMeasuringModule.SetTargetBuffers();
            }
            else
            {
                _auxDirector.SetMeasurementRenderTargets(_lineMeasuringModule.RenderTargets);
                _auxDirector.StartInternal();
            }

            CameraDirector.playableAsset = OneTestConfiguration.CameraTimelineAsset;
            var ut = OneTestConfiguration.CameraTimelineAsset.GetOutputTracks().ToList();
            CameraDirector.SetGenericBinding(ut[1], cam.GetComponent<Animator>());

            MeasurementUtils.SetAnimationToMeasurement(CameraDirector, OneTestConfiguration.SequenceFramesCount, _requestedTestFrame);

            if (OneTestConfiguration.LightTimelineAsset != null)
            {
                LightDirector.playableAsset = OneTestConfiguration.LightTimelineAsset;
                var ut2 = OneTestConfiguration.LightTimelineAsset.GetOutputTracks().ToList();
                LightDirector.SetGenericBinding(ut2[1], PointLightObject.GetComponent<Animator>());
                MeasurementUtils.SetAnimationToMeasurement(LightDirector, OneTestConfiguration.SequenceFramesCount, _requestedTestFrame);
            }

            _runnerSupports.ForEach(c => c.MyStart(_lineMeasuringModule));
            var requiredGauges = _runnerSupports.SelectMany(c => c.RequiredGauges()).Distinct().ToList();

            _gauges = MeasurementUtils.CreateGauges(SkeletonizerMaterial,requiredGauges);

        }

        public void Destroy()
        {
            _lineMeasuringModule.MyDestroy();

        }

        public void OnPreRender()
        {
            _auxDirector?.OnPreRenderInternal();
        }

        public void Update()
        {
            _runnerSupports.ForEach(c => c.MyOnUpdate());
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            _auxDirector?.OnRenderImageInternal(src,dest);

            if (InteractionMode == MTestInteractionMode.Animation && _lastMeasurementFrame != _requestedTestFrame)
            {
                MeasurementUtils.SetAnimationToMeasurement(CameraDirector, OneTestConfiguration.SequenceFramesCount, _requestedTestFrame);
                if (OneTestConfiguration.LightTimelineAsset != null)
                {
                    MeasurementUtils.SetAnimationToMeasurement(LightDirector, OneTestConfiguration.SequenceFramesCount, _requestedTestFrame);
                }

                _lastMeasurementFrame = _requestedTestFrame;
            }

            if (_forceMeasurement)
            {
                MakeMeasurement();
                _forceMeasurement = false;
            }

            var dstIsSet = _runnerSupports.Select(c => c.MyOnRenderImage(src, dest)).ToList().Any(c => c);

            if (!dstIsSet)
            {
                Graphics.Blit(src,dest);
            }

            _runnerSupports.ForEach(c => c.MyLateOnRenderImage(src, dest));
        }

        private void MakeMeasurement()
        {
            if (!_gauges.Any())
            {
                return;
            }
            var msw = new MyStopWatch();
            var lastTestFrame = OneTestConfiguration.TestFramesCount + OneTestConfiguration.FirstTestFrame;
            msw.StartSegment($"Test {OneTestConfiguration.TestName} frame {_requestedTestFrame} = {_requestedTestFrame-OneTestConfiguration.FirstTestFrame}/{lastTestFrame}");
            MeasurementScreenshotsSet set = _lineMeasuringModule.OnRenderImageGenerateScreenshots();

            var measurementResults = new List<IMeasurementResult>();
            foreach (var aGauge in _gauges)
            {
                var measurementResult = aGauge.TakeMeasurement(set);
                measurementResults.Add(measurementResult);
            }

            _runnerSupports.ForEach(c => c.MyOnMeasurementsMade(measurementResults, set));

            Debug.Log(msw.CollectResults());
        }

        public enum MTestInteractionMode
        {
            Animation, FreeFloating
        }

        public void Enable(MOneTestConfiguration configuration, INprRenderingPostProcessingDirector ppDirector, List<IMRunnerSupport> supports)
        {
            this.enabled = true;
            OneTestConfiguration = configuration;
            _auxDirector = ppDirector;
            _runnerSupports = supports;
        }

        public bool TestFinished => _lastMeasurementFrame >= OneTestConfiguration.FirstTestFrame+ OneTestConfiguration.TestFramesCount;

        public int RequestedTestFrame
        {
            get => _requestedTestFrame;
            set => _requestedTestFrame = value;
        }

        public void ForceMeasurement()
        {
            _forceMeasurement = true;
        }

        public MeasurementRenderTargetsSet RenderTargets => _lineMeasuringModule.RenderTargets;
    }
}