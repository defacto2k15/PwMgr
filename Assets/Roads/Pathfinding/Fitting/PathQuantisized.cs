using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Engraving;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using UnityEngine;

namespace Assets.Roads.Pathfinding.Fitting
{
    public class PathQuantisized
    {
        private readonly IGeometry _line;

        public PathQuantisized(IGeometry line)
        {
            _line = line;
        }


        public PathQuantisized(List<Vector2> points)
        {
            _line = new LineString(points.Select(c => MyNetTopologySuiteUtils.ToCoordinate(c)).ToArray());
        }

        public PathProximityInfo ProximityAtPoint(Vector2 position)
        {
            var op = new DistanceOp(_line, MyNetTopologySuiteUtils.ToPoint(position));
            var toReturn = new PathProximityInfo()
            {
                Distance = (float) op.Distance(),
                ToCenterDelta = position - MyNetTopologySuiteUtils.ToVector2(op.NearestPoints().First())
            };
            return toReturn;
        }

        public PathQuantisized CutSubRectangle(MyRectangle mapCoords)
        {
            var envelope = MyNetTopologySuiteUtils.ToPolygon(mapCoords);
            return new PathQuantisized(_line.Intersection(envelope));
        }

        public MyRectangle Envelope => _line.EnvelopeInternal.ToUnityCoordPositions2D();

        public IGeometry Line => _line;

        public List<Vector2> PathNodes => (_line as ILineString).CoordinateSequence.ToCoordinateArray()
            .Select(c => MyNetTopologySuiteUtils.ToVector2(c)).ToList();
    }
}