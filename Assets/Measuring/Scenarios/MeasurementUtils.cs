using System.Collections.Generic;
using Assets.Measuring.Gauges;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Playables;

namespace Assets.Measuring.Scenarios
{
    public static class MeasurementUtils
    {
        public static void SetAnimationToMeasurement(PlayableDirector director, int measurementsToMake, int measurementIndex)
        {
            Debug.Log($"M782 animation to measurement {measurementIndex}/{measurementsToMake}");
            float percent = measurementIndex / (float)measurementsToMake;
            director.time = director.duration * percent;
            director.Evaluate();
        }

        public static List<IGauge> CreateGauges(Material skeletonizerMaterial, List<GaugeType> gaugeTypes)
        {
            var screenSize = new IntVector2(Screen.width, Screen.height);
            var blockSize = new IntVector2(16, 16);
            var blockCount = new IntVector2(Mathf.CeilToInt(screenSize.X / (float) blockSize.X), Mathf.CeilToInt(screenSize.Y / (float) blockSize.Y));

            var divisionSettings = new GridDivisionSettings()
            {
                BlockCount = blockCount,
                BlockSize = blockSize,
                ScreenSize = screenSize
            };

            var gauges = new List<IGauge>();
            if (gaugeTypes.Contains(GaugeType.BlockSpecificationGauge))
            {
                gauges.Add(new BlockSpecificationGauge(divisionSettings));
            }

            if (gaugeTypes.Contains(GaugeType.LinesLayoutGauge))
            {
                gauges.Add(new LinesLayoutGauge());
            }

            if (gaugeTypes.Contains(GaugeType.LinesWidthGauge))
            {
                gauges.Add(new LinesWidthGauge(skeletonizerMaterial));
            }

            if (gaugeTypes.Contains(GaugeType.StrokesPixelCountGauge))
            {
                gauges.Add(new StrokesPixelCountGauge());
            }

            return gauges;
        }
    }
}