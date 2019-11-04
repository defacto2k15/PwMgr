using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearRegression;
using UnityEngine;

namespace Assets.Roads.Pathfinding.Fitting
{
    public class PathFitterDebugObject : MonoBehaviour
    {
        private List<DebugFittingResult> _fittingResults;
        private List<Vector2> _inputPoints;

        public void Start()
        {
            List<Func<double[], double[], double[], Func<double, double>>> interpolatingFunctions =
                new List<Func<double[], double[], double[], Func<double, double>>>()
                {
                    //(tArr, vArr, wArr) => (t) => Interpolate.CubicSpline(tArr, vArr).Interpolate(t),
                    (tArr, vArr, wArr) => (t) => Evaluate.Polynomial(t, Fit.PolynomialWeighted(tArr, vArr, wArr, 3)),
                    //(tArr, vArr, wArr) => (t) => Fit.PolynomialFunc(tArr, vArr, 4)(t),
                };
            var inputData = new List<Vector2>()
            {
                new Vector2(5, 5),
                new Vector2(10, 9),
                new Vector2(12, 6),
                new Vector2(-2, 2),
                new Vector2(-4, 7),
            };

            var weights = new List<double>()
            {
                0.25,
                0.5,
                1,
                0.5,
                0.25
            };

            _fittingResults = new List<DebugFittingResult>();

            foreach (var function in interpolatingFunctions)
            {
                _fittingResults.Add(TestFitting(function, inputData, weights));
            }
            _inputPoints = inputData;
        }

        public void OnDrawGizmosSelected()
        {
            if (_fittingResults != null)
            {
                Gizmos.color = Color.red;
                foreach (var inputPoint in _inputPoints)
                {
                    Gizmos.DrawSphere(new Vector3(inputPoint.x, inputPoint.y, 0), 0.1f);
                }

                for (int k = 0; k < _fittingResults.Count; k++)
                {
                    Gizmos.color = new Color((float) k / _fittingResults.Count, 0, 0);
                    var samples = _fittingResults[k].ResultSamples;
                    for (int i = 0; i < samples.Count - 1; i++)
                    {
                        var sample1 = samples[i];
                        var sample2 = samples[i + 1];

                        var pos1 = new Vector3(sample1.x, sample1.y, 0);
                        var pos2 = new Vector3(sample2.x, sample2.y, 0);

                        Gizmos.DrawLine(pos1, pos2);
                    }
                }
            }
        }

        public Func<double, Vector2> CreateFunction(List<Vector2> pointPositions, List<double> weights,
            Func<double[], double[], double[], Func<double, double>> interpolatingFunction)
        {
            double[] xdata = pointPositions.Select(c => (double) c.x).ToArray();
            double[] ydata = pointPositions.Select(c => (double) c.y).ToArray();
            double[] tdata = pointPositions.Select((_, i) => (double) i).ToArray();

            var p = Fit.PolynomialFunc(xdata, ydata, 3);
            return (t) =>
                new Vector2(
                    (float) interpolatingFunction(tdata, xdata, weights.ToArray())(t),
                    (float) interpolatingFunction(tdata, ydata, weights.ToArray())(t));
            // trik z https://www.codeproject.com/Articles/560163/Csharp-Cubic-Spline-Interpolation
        }


        public DebugFittingResult TestFitting(
            Func<double[], double[], double[], Func<double, double>> interpolatingFunction,
            List<Vector2> inputPositions, List<double> weights)
        {
            var func = CreateFunction(inputPositions, weights, interpolatingFunction);
            var tRange = (float) inputPositions.Count - 1;
            var samplesCount = 30;
            List<Vector2> samples = new List<Vector2>();
            for (int i = 0; i < samplesCount; i++)
            {
                var x = (tRange) * (i / (float) samplesCount);
                var funcValue = func(x);
                samples.Add(funcValue);
            }
            return new DebugFittingResult()
            {
                ResultSamples = samples
            };
        }


        public class DebugFittingResult
        {
            public List<Vector2> ResultSamples;
        }
    }
}