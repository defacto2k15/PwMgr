using System;
using System.Linq;
using Assets.TerrainMat;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using UnityEngine;
using Envelope = GeoAPI.Geometries.Envelope;

namespace Assets.Utils
{
    public class MyPolygon
    {
        private readonly Vector2[] _points;

        public MyPolygon(Vector2[] points)
        {
            _points = points;
        }

        public MyPolygon Multiply(float multiplier)
        {
            return new MyPolygon(_points.Select(c => c * multiplier).ToArray());
        }

        public bool IsIn(Vector2 point)
        {
            return Pos2DUtils.IsPointInPolygon(point, _points);
        }

        public static MyPolygon Create(params Vector2[] points)
        {
            return new MyPolygon(points);
        }

        public Vector2[] Points
        {
            get { return _points; }
        }

        public MyPolygon MoveBy(Vector2 delta)
        {
            return new MyPolygon(_points.Select(c => c + delta).ToArray());
        }

        public static MyPolygon RectangleWithBorder(float x1, float x2, float y1, float y2)
        {
            float margin = 0.01f;
            return new MyPolygon(new[]
            {
                new Vector2(x1 - margin, y1 - margin),
                new Vector2(x1 - margin, y2 + margin),
                new Vector2(x2 + margin, y2 + margin),
                new Vector2(x2 + margin, y1 - margin),
            });
        }

        public GeoAPI.Geometries.Envelope CalculateEnvelope()
        {
            var toReturn = new Envelope(
                _points.OrderBy(c => c.x).Select(c => c.x).First(),
                _points.OrderBy(c => c.y).Select(c => c.y).First(),
                _points.OrderByDescending(c => c.x).Select(c => c.x).First(),
                _points.OrderByDescending(c => c.y).Select(c => c.y).First()
            );
            return toReturn;
        }

        public override string ToString()
        {
            return $"{nameof(Points)}: {StringUtils.ToString(Points)}";
        }
    }
}