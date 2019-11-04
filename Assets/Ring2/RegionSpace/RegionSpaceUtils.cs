using System.Collections.Generic;
using System.Linq;
using Assets.Ring2.Geometries;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using UnityEngine;

namespace Assets.Ring2.RegionSpace
{
    public static class RegionSpaceUtils
    {
        public static IRegionSpace Create(IGeometry geometry)
        {
            if (geometry is IPolygon)
            {
                return new PolygonRegionSpace(geometry as IPolygon);
            }
            else if (geometry is IMultiPolygon)
            {
                var mp = geometry as IMultiPolygon;
                return new MultipolygonRegionSpace(
                    Enumerable.Range(0, mp.Count)
                        .Select(i => mp.GetGeometryN(i))
                        .Cast<IPolygon>()
                        .ToList());
            }
            else
            {
                Preconditions.Fail("Cannot Create space from geometry of type " + geometry.GetType());
                return null;
            }
        }

        public static IRegionSpace ToFatLineString(float width, IEnumerable<Vector2> points)
        {
            var lineString = new LineString(points.Select(MyNetTopologySuiteUtils.ToCoordinate).ToArray());
            return new FatLineString(lineString, width);
        }
    }
}