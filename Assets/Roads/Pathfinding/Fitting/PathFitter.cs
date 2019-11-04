using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using UnityEngine;

namespace Assets.Roads.Pathfinding.Fitting
{
    public class PathFitter
    {
        public PathCurve FitPath(List<Vector2> path, int windowSizeCount, int polynomialOrder)
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

            return PathCurve.Create(path, subCurveFunctions, windowSizeCount);
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
    }
}