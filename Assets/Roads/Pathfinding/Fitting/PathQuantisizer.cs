using System.Linq;
using Assets.TerrainMat;
using NetTopologySuite.Geometries;
using UnityEngine;

namespace Assets.Roads.Pathfinding.Fitting
{
    public class PathQuantisizer
    {
        private float _samplesPerUnit;

        public PathQuantisizer(float samplesPerUnit)
        {
            _samplesPerUnit = samplesPerUnit;
        }

        public PathQuantisized GenerateQuantisizedPath(PathCurve pureCurve)
        {
            var tapeSum = pureCurve.TapeSum;
            var samplesCount = Mathf.RoundToInt(tapeSum * _samplesPerUnit);

            var samples = Enumerable.Range(0, samplesCount).Select(i => (float) i / (samplesCount - 1))
                .Select(t => pureCurve.Sample(t));

            var line = new LineString(samples.Select(c => MyNetTopologySuiteUtils.ToCoordinate(c)).ToArray());
            return new PathQuantisized(line);
        }
    }
}