using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.Geometries;
using Assets.Utils;
using GeoAPI.Geometries;
using GeoAPI.Operation.Buffer;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Distance;
using UnityEngine;

namespace Assets.TerrainMat
{
    public static class MyNetTopologySuiteUtils
    {
        private static readonly GeometryFactory Factory = new GeometryFactory();

        public static IPolygon ToPolygon(MyPolygon intrestingPolygon)
        {
            var arraySize = intrestingPolygon.Points.Count() + 1;
            var coordinatesArray = new Coordinate[arraySize];
            for (int i = 0; i < arraySize - 1; i++)
            {
                coordinatesArray[i] = ToCoordinate(intrestingPolygon.Points[i]);
            }
            coordinatesArray[arraySize - 1] = ToCoordinate(intrestingPolygon.Points[0]);
            return Factory.CreatePolygon(coordinatesArray);
        }

        public static IPolygon ToPolygon(Vector2[] points)
        {
            var arraySize = points.Count() + 1;
            var coordinatesArray = new Coordinate[arraySize];
            for (int i = 0; i < arraySize - 1; i++)
            {
                coordinatesArray[i] = ToCoordinate(points[i]);
            }
            coordinatesArray[arraySize - 1] = ToCoordinate(points[0]);
            return Factory.CreatePolygon(coordinatesArray);
        }

        public static IPolygon ToPolygonWothJoinedEndPoints(IEnumerable<Vector2> points)
        {
            return Factory.CreatePolygon(points.Select(ToCoordinate).ToArray());
        }

        public static IPolygon ToPolygon(MyRectangle rect)
        {
            return ToPolygon(new Vector2[]
            {
                new Vector2(rect.X, rect.Y),
                new Vector2(rect.X + rect.Width, rect.Y),
                new Vector2(rect.X + rect.Width, rect.Y + rect.Height),
                new Vector2(rect.X, rect.Y + rect.Height),
            });
        }

        public static MyPolygon ToMyPolygon(IPolygon polygon)
        {
            Preconditions.Assert(polygon.NumInteriorRings == 0,
                $"Polygon dont have exacly zero interior ring! There is {polygon.NumInteriorRings}");
            var ring = polygon.ExteriorRing;
            var points = Enumerable.Range(0, ring.NumPoints).Select(i => ring.GetPointN(i)).Select(p => ToVector2(p))
                .ToArray();
            return new MyPolygon(points);
        }

        public static Coordinate ToCoordinate(Vector2 input)
        {
            return new Coordinate(input.x, input.y);
        }

        public static Envelope ToPointEnvelope(Vector2 pos, float smallLength = 0.01f)
        {
            return new Envelope(pos.x - smallLength, pos.x + smallLength, pos.y - smallLength, pos.y + smallLength);
        }

        public static Point ToPoint(Vector2 position2D)
        {
            return new Point(position2D.x, position2D.y);
        }

        public static Vector2 ToVector2(Coordinate coordinate)
        {
            return new Vector2((float) coordinate.X, (float) coordinate.Y);
        }

        public static Vector2 ToVector2(IPoint point)
        {
            return new Vector2((float) point.X, (float) point.Y);
        }

        public static IGeometry ToGeometryEnvelope(Envelope envelope)
        {
            var coordinatesArray = new Coordinate[5];
            coordinatesArray[0] = new Coordinate(envelope.MinX, envelope.MinY);
            coordinatesArray[1] = new Coordinate(envelope.MinX, envelope.MaxY);
            coordinatesArray[2] = new Coordinate(envelope.MaxX, envelope.MaxY);
            coordinatesArray[3] = new Coordinate(envelope.MaxX, envelope.MinY);
            coordinatesArray[4] = new Coordinate(envelope.MinX, envelope.MinY);
            return Factory.CreatePolygon(coordinatesArray);
        }

        public static IGeometry ToGeometryEnvelope(MyRectangle queryRectange)
        {
            return ToGeometryEnvelope(ToEnvelope(queryRectange));
        }

        public static Envelope ToEnvelope(MyRectangle rect)
        {
            return new Envelope(rect.X, rect.X + rect.Width, rect.Y, rect.Y + rect.Height);
        }

        public static Envelope ToEnvelope(List<Vector2> points)
        {
            return new Envelope(
                points.Min(c => c.x),
                points.Max(c => c.x),
                points.Min(c => c.y),
                points.Max(c => c.y)
            );
        }

        public static Envelope UnionEnvelope(List<Envelope> envelopes)
        {
            Envelope toReturn = null;
            //try
            //{
            toReturn = new Envelope(
                envelopes.Min(c => c.MinX),
                envelopes.Max(c => c.MaxX),
                envelopes.Min(c => c.MinY),
                envelopes.Max(c => c.MaxY)
            );
            //}
            //catch (Exception e)
            //{
            //    int tt = 2;
            //}

            return toReturn;
        }

        public static List<IPolygon> ToSinglePolygons(IGeometry geo)
        {
            if (geo is IPolygon)
            {
                return new List<IPolygon>()
                {
                    geo as IPolygon
                };
            }
            else if (geo is IMultiPolygon)
            {
                var multipolygon = (IMultiPolygon) geo;
                return multipolygon.Geometries.SelectMany(c => ToSinglePolygons(c)).ToList();
            }
            else
            {
                Preconditions.Fail($"Geo is not polygon nor multipolygon. type if {geo.GetType()} {geo}");
                return null;
            }
        }

        public static IGeometry EnlargeByMargin(IGeometry geometry, float margin)
        {
            var bufObj = new BufferOp(geometry, new BufferParameters(0, EndCapStyle.Square));
            return bufObj.GetResultGeometry(margin);
        }

        public static float Distance(IGeometry geo1, IGeometry geo2, float maxDistance)
        {
            var op = new DistanceOp(geo1, geo2, maxDistance);
            return (float) op.Distance();
        }

        public static float Distance(IGeometry geo1, Vector2 point, float maxDistance)
        {
            return Distance(geo1, MyNetTopologySuiteUtils.ToPoint(point), maxDistance);
        }

        public static IPolygon CreateRectanglePolygon(float halfLength)
        {
            var myPolygon =
                Assets.Utils.MyPolygon.RectangleWithBorder(-halfLength, halfLength, -halfLength, halfLength);
            var ntsPolygon = MyNetTopologySuiteUtils.ToPolygon(myPolygon);
            return ntsPolygon;
        }

        public static IGeometry CloneWithTransformation(IGeometry geometry, AffineTransformation transformation)
        {
            return transformation.Transform(geometry.Clone() as IGeometry);
        }

        public static IGeometry CreateConvexHullFromPoints(Vector2[] points)
        {
            var multiPoint = new MultiPoint(points.Select(c => (IPoint) ToPoint(c)).ToArray());
            return multiPoint.ConvexHull();
        }
    }
}