using System.Collections.Generic;
using System.IO;
using Assets.Utils;
using UnityEngine;

namespace Assets.Measuring.Scenarios
{
    public class FrameMeasurementsTaker
    {
        private List<IGauge> _gauges;
        private string _savePath;

        public FrameMeasurementsTaker(List<IGauge> gauges, string savePath)
        {
            _gauges = gauges;
            _savePath = savePath;
        }

        public void TakeMeasurements(int frameNo, MeasurementScreenshotsSet screenshotsSet)
        {
            Debug.Log("R827 RESOLUTION: "+screenshotsSet.HatchMainTexture.Height + " "+screenshotsSet.HatchMainTexture.Width);
            var msw = new MyStopWatch();
            foreach (var gauge in _gauges)
            {
                msw.StartSegment(gauge.ToString());
                var result = gauge.TakeMeasurement(screenshotsSet);
                WriteResultsToFile(frameNo, result);
            }
            Debug.Log(msw.CollectResults());
        }

        private void WriteResultsToFile(int frameNo, IMeasurementResult result)
        {
            File.WriteAllText($"{_savePath}/{result.GetResultName()}.{frameNo}.csv", result.ToCsvString());
        }
    }
}