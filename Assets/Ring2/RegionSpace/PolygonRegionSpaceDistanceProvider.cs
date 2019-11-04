using Assets.TerrainMat;
using GeoAPI.Geometries;
using NetTopologySuite.Operation.Distance;
using UnityEngine;

namespace Assets.Ring2.RegionSpace
{
    public class PolygonRegionSpaceDistanceProvider : IRegionSpaceDistanceProvider
    {
        private readonly IPolygon _polygon;
        private ILinearRing _shell;

        public PolygonRegionSpaceDistanceProvider(IPolygon polygon)
        {
            _polygon = polygon;
            _shell = polygon.Shell;
        }

        public float GetDistanceAt(Vector2 position)
        {
            var polyOp = new DistanceOp(_polygon, MyNetTopologySuiteUtils.ToPoint(position));
            var polyLength = (float) polyOp.Distance();

            var op = new DistanceOp(_shell, MyNetTopologySuiteUtils.ToPoint(position));
            var shellLength = (float) op.Distance();
            if (shellLength - polyLength > 0.01)
            {
                return shellLength;
            }
            else
            {
                return -1; //inside
            }
        }
    }
}