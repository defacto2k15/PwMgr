using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Roads.Osm
{
    public class GeoCoordsRect
    {
        private GeoCoordinate _downLeftCoord;
        private GeoCoordinate _topRightCoord;

        public GeoCoordsRect(GeoCoordinate downLeftCoord, GeoCoordinate topRightCoord)
        {
            _downLeftCoord = downLeftCoord;
            _topRightCoord = topRightCoord;
        }

        public bool Contains(GeoCoordinate other)
        {
//latitude is Y
            return _downLeftCoord.Latitude <= other.Latitude &&
                   _downLeftCoord.Longitude <= other.Longitude &&
                   _topRightCoord.Latitude >= other.Latitude &&
                   _topRightCoord.Longitude >= other.Longitude;
        }

        public Vector2 Size => new Vector2(
            (float) (_topRightCoord.Longitude - _downLeftCoord.Longitude),
            (float) (_topRightCoord.Latitude - _downLeftCoord.Latitude));

        public GeoCoordinate DownLeftCoord => _downLeftCoord;
        public GeoCoordinate TopLeftCoord => new GeoCoordinate(_topRightCoord.Latitude, _downLeftCoord.Longitude);
        public GeoCoordinate DownRightCoord => new GeoCoordinate(_downLeftCoord.Latitude, _topRightCoord.Longitude);
    }
}