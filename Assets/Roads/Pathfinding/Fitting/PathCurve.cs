using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Roads.Pathfinding.Fitting
{
    public class PathCurve
    {
        private List<PointWithTapeDistance> _pathPoints;
        private List<CurveFunc> _subCurveFunctions;
        private float _tapeSum;
        private int _subCurveSpan; //how many points

        private PathCurve(List<PointWithTapeDistance> pathPoints, List<CurveFunc> subCurveFunctions, float tapeSum,
            int subCurveSpan)
        {
            _pathPoints = pathPoints;
            _subCurveFunctions = subCurveFunctions;
            _tapeSum = tapeSum;
            _subCurveSpan = subCurveSpan;
        }

        public Vector2 Sample(float t)
        {
            var tapeMoment = t * _tapeSum;
            int startPointIndex = 0;
            foreach (var point in _pathPoints)
            {
                if (point.StartTapeDistance + point.SectionTapeDistance >= tapeMoment)
                {
                    break;
                }
                else
                {
                    startPointIndex++;
                }
            }
            startPointIndex = Math.Min(startPointIndex, _pathPoints.Count - 1);

            PointWithTapeDistance section = _pathPoints[startPointIndex];

            var inSectionPercent = (tapeMoment - section.StartTapeDistance) / section.SectionTapeDistance;

            var usedSubCurves = new List<InstancedCurve>();


            for (int subCurveIndex = 0; subCurveIndex < _subCurveSpan - 1; subCurveIndex++)
            {
                var curveIndex = startPointIndex + (subCurveIndex - (_subCurveSpan - 2));
                if (curveIndex < 0)
                {
                    continue;
                }
                else if (curveIndex >= _subCurveFunctions.Count)
                {
                    continue;
                }

                usedSubCurves.Add(new InstancedCurve()
                {
                    CurveFunc = _subCurveFunctions[curveIndex],
                    Offset = (1f - (1f / (_subCurveSpan - 1)) * (subCurveIndex + 1)),
                    UsedPointFirstIndex = curveIndex
                });
            }


            List<SampleWithWeight> samples = new List<SampleWithWeight>();
            foreach (var subCurve in usedSubCurves)
            {
                var subCurvePercent = subCurve.Offset + (inSectionPercent / (_subCurveSpan - 1));
                var sampleValue = subCurve.CurveFunc.Sample(subCurvePercent);
                var weight = 0f;

                var curveStartPoint = _pathPoints[subCurve.UsedPointFirstIndex];
                var curvePreLast = _pathPoints[subCurve.UsedPointFirstIndex + _subCurveSpan - 1 - 1];

                var curveTapeDistance = curvePreLast.EndTapeDistance - curveStartPoint.StartTapeDistance;
                var inCurvePosition = tapeMoment - curveStartPoint.StartTapeDistance;
                var curvePercent = inCurvePosition / curveTapeDistance;

                if (subCurve.Offset == 0.5)
                {
                    weight = (1f - inSectionPercent);
                }
                else
                {
                    weight = inSectionPercent;
                }

                var finalCurvePercent = 0f;
                if (curvePercent < 0.5f)
                {
                    finalCurvePercent = curvePercent * 2f;
                }
                else
                {
                    finalCurvePercent = (-curvePercent + 1f) * 2f;
                }

                samples.Add(new SampleWithWeight()
                {
                    Sample = sampleValue,
                    Weight = finalCurvePercent
                });
            }

            if (samples.Count > 1)
            {
                var weightsSum = samples.Sum(c => c.Weight);
                samples.ForEach(s => s.Weight /= weightsSum);

                Vector2 sum = Vector2.zero;
                foreach (var sample in samples)
                {
                    sum += sample.Sample * sample.Weight;
                }
                return sum;
            }
            else
            {
                if (samples.Count == 0)
                {
                    int todo = 2;
                }
                return samples[0].Sample;
            }
        }

        public float TapeSum => _tapeSum;

        private class InstancedCurve
        {
            public CurveFunc CurveFunc;
            public double Offset;
            public int UsedPointFirstIndex;
        }

        private class SampleWithWeight
        {
            public Vector2 Sample;
            public float Weight;
        }

        public static PathCurve Create(List<Vector2> pathPoints, List<CurveFunc> subCurveFunctions, int curveWindowSize)
        {
            var pointWithTapes = new List<PointWithTapeDistance>();
            float tapeSum = 0;
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                var p1 = pathPoints[i];
                var p2 = pathPoints[i + 1];

                var distance = Vector2.Distance(p1, p2);

                pointWithTapes.Add(new PointWithTapeDistance()
                {
                    Point = p1,
                    StartTapeDistance = tapeSum,
                    SectionTapeDistance = distance
                });

                tapeSum += distance;
            }

            return new PathCurve(pointWithTapes, subCurveFunctions, tapeSum, curveWindowSize);
        }

        private class PointWithTapeDistance
        {
            public Vector2 Point;
            public float StartTapeDistance;
            public float SectionTapeDistance;

            public float EndTapeDistance => StartTapeDistance + SectionTapeDistance;
        }
    }
}