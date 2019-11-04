using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Roads.Pathfinding.Fitting;
using MathNet.Numerics;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class ImageGenPathDebugObject : MonoBehaviour
    {
        public DebugDrawPathCurve FitPath(List<Vector2> path, int windowSizeCount, int polynomialOrder)
        {
            var windowSize = new WindowSize(windowSizeCount);
            var subCurveFunctions = new List<CurveFunc>();

            for (int i = 0; i <= path.Count - windowSize.FullSize; i++)
            {
                var subcurvePoints = path.Skip(i).Take(windowSize.FullSize).ToList();

                var xArray = subcurvePoints.Select(c => (double) c.x).ToArray();
                var yArray = subcurvePoints.Select(c => (double) c.y).ToArray();
                var tArray = Enumerable.Range(0, subcurvePoints.Count)
                    .Select(c => (double) c / (subcurvePoints.Count - 1)).ToArray();

                var xFunc = Fit.PolynomialFunc(tArray, xArray, polynomialOrder);
                var yFunc = Fit.PolynomialFunc(tArray, yArray, polynomialOrder);

                subCurveFunctions.Add(new CurveFunc((t) => new Vector2((float) xFunc(t), (float) yFunc(t))));
            }

            return new DebugDrawPathCurve()
            {
                FinalCurve = PathCurve.Create(path, subCurveFunctions, windowSizeCount),
                SubCurves = subCurveFunctions
            };
        }

        private class WindowSize
        {
            private int _fullSize;

            public WindowSize(int fullSize)
            {
                _fullSize = fullSize;
            }

            public int FullSize => _fullSize;
        }

        public class DebugDrawPathCurve
        {
            public PathCurve FinalCurve;
            public List<CurveFunc> SubCurves;
        }

        /// ////////////////////////////////////////////////
        private List<List<Vector2>> _subCurveSamples;

        private List<Vector2> _mainSamples;
        private List<Vector2> _nodes;

        public void Start()
        {
            _nodes = new List<Vector2>()
            {
                new Vector2(5, 5),
                new Vector2(10, 3),
                new Vector2(13, 7),
                new Vector2(18, 4),
                new Vector2(22, 9),
                new Vector2(26, 19),
                new Vector2(2, 25),
            };


            var debugPathCurve = FitPath(_nodes, 5, 3);

            int samplesCount = 70;
            _mainSamples = new List<Vector2>();
            for (int i = 0; i < samplesCount; i++)
            {
                var t = (float) i / (samplesCount - 1);
                _mainSamples.Add(debugPathCurve.FinalCurve.Sample(t));
            }

            _subCurveSamples = new List<List<Vector2>>();
            foreach (var subcurve in debugPathCurve.SubCurves)
            {
                var samples = new List<Vector2>();
                for (int i = 0; i < samplesCount; i++)
                {
                    var t = (float) i / (samplesCount - 1);
                    samples.Add(subcurve.Sample(t));
                }
                _subCurveSamples.Add(samples);
            }
        }

        public void OnDrawGizmosSelected()
        {
            if (_nodes != null)
            {
                Gizmos.color = Color.red;
                foreach (var node in _nodes)
                {
                    Gizmos.DrawSphere(new Vector3(node.x, node.y, 0), 0.1f);
                }

                Gizmos.color = Color.black;
                for (int i = 0; i < _mainSamples.Count - 1; i++)
                {
                    var p1 = _mainSamples[i];
                    var p2 = _mainSamples[i + 1];

                    for (int k = 0; k < 5; k++)
                    {
                        Gizmos.DrawLine(new Vector3(p1.x, p1.y + 0.02f * k, 0), new Vector3(p2.x, p2.y + 0.02f * k, 0));
                    }
                }

                int subIndex = 0;
                foreach (var subcurveSamples in _subCurveSamples)
                {
                    Gizmos.color = new Color(0, 0, 0.2f + subIndex / 6f);
                    for (int i = 0; i < subcurveSamples.Count - 1; i++)
                    {
                        var p1 = subcurveSamples[i];
                        var p2 = subcurveSamples[i + 1];

                        Gizmos.DrawLine(new Vector3(p1.x, p1.y, 0), new Vector3(p2.x, p2.y, 0));
                    }
                    subIndex++;
                }
            }
        }
    }
}