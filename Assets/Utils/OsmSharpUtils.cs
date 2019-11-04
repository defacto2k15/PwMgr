using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Utils
{
    public static class OsmSharpUtils
    {
        public static Vector2 ToVector2(this GeoCoordinate coord)
        {
            return new Vector2((float) coord.Longitude, (float) coord.Latitude);
        }

        public static GeoCoordinate ToGeoCoordinate(this Vector2 coord)
        {
            return new GeoCoordinate(coord.y, coord.x);
        }
    }
}