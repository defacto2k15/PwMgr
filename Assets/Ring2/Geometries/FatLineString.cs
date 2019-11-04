using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Ring2.RegionSpace;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using UnityEngine;

namespace Assets.Ring2.Geometries
{
    public class FatLineString : IRegionSpace
    {
        private LineString _lineString;
        private float _widthOfLine;

        public FatLineString(LineString lineString, float widthOfLine)
        {
            _lineString = lineString;
            _widthOfLine = widthOfLine;
        }

        public Envelope Envelope => EnvelopeUtils.WidenEnvelope(_lineString.EnvelopeInternal, _widthOfLine);

        public IRegionSpaceDistanceProvider DistanceProvider => new FatLineStringDistanceProvider(_lineString,
            _widthOfLine);

        public bool IsEmpty
        {
            get { return false; }
        }

        public bool Intersects(Envelope envelope)
        {
            return _lineString.Intersects(
                MyNetTopologySuiteUtils.ToGeometryEnvelope(
                    EnvelopeUtils.WidenEnvelope(envelope, _widthOfLine)));
        }
    }

    public class FatLineStringDistanceProvider : IRegionSpaceDistanceProvider
    {
        private readonly LineString _lineString;
        private readonly float _widthOfLine;

        public FatLineStringDistanceProvider(LineString lineString, float widthOfLine)
        {
            _lineString = lineString;
            _widthOfLine = widthOfLine;
        }

        public float GetDistanceAt(Vector2 position)
        {
            var op = new DistanceOp(_lineString, MyNetTopologySuiteUtils.ToPoint(position));
            var length = (float) op.Distance();

            var half = _widthOfLine / 2;
            if (length > half)
            {
                return -1;
            }
            else
            {
                return half - length;
            }
        }
    }
}