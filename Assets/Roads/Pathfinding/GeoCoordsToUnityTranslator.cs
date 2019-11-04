using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Osm;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Roads.Pathfinding
{
    public class GeoCoordsToUnityTranslator
    {
        //private UnityCoordsPositions2D _mapUnityCoords;
        //private GeoCoordsRect _mapGeoCoords;
        //private Vector2 _realLifeMapSize;

        private readonly GeoCoordsTranslatorRootPoint _rootPoint;
        private Vector2 _geoUnitsInUnitySize;

        public GeoCoordsToUnityTranslator(
            MyRectangle mapUnityCoords, GeoCoordsRect mapGeoCoords,
            GeoCoordsTranslatorRootPoint rootPoint
        )
        {
            //_mapUnityCoords = mapUnityCoords;
            //_mapGeoCoords = mapGeoCoords;
            _rootPoint = rootPoint;

            //var totalHeightInMeters = (float)mapGeoCoords.DownLeftCoord.DistanceReal(_mapGeoCoords.TopLeftCoord).Value;
            //var totalWidthInMeters = (float)mapGeoCoords.DownLeftCoord.DistanceReal(_mapGeoCoords.DownRightCoord).Value;
            //_realLifeMapSize = new Vector2(totalWidthInMeters, totalHeightInMeters);

            var geoSize = mapGeoCoords.Size;
            var unitySize = mapUnityCoords.Size;

            _geoUnitsInUnitySize = new Vector2(unitySize.x / geoSize.x, unitySize.y / geoSize.y);
        }

        public Vector2 TranslateToUnity(Vector2 inputCoordinate)
        {
            return TranslateToUnity(new GeoCoordinate(inputCoordinate.y, inputCoordinate.x));
        }

        public Vector2 TranslateToUnity(GeoCoordinate inputCoordinate)
        {
//latitude is y 

            var rootDelta = inputCoordinate.ToVector2() - _rootPoint.GeoCoord.ToVector2();

            var deltaInUnityUnits = new Vector2(rootDelta.x * _geoUnitsInUnitySize.x,
                rootDelta.y * _geoUnitsInUnitySize.y);

            var toReturn = _rootPoint.UnityPosition + deltaInUnityUnits;
            return toReturn;

            //var inputOnVerticalLine = new GeoCoordinate(inputCoordinate.Latitude, _mapGeoCoords.DownLeftCoord.Longitude);
            //var yDistance = (float)_mapGeoCoords.DownLeftCoord.DistanceReal(inputOnVerticalLine).Value;
            //var yUv = yDistance / _realLifeMapSize.y;

            //var inputOnHorizontalLine = new GeoCoordinate(_mapGeoCoords.DownLeftCoord.Latitude,
            //    inputCoordinate.Longitude);
            //var xDistance = (float)_mapGeoCoords.DownLeftCoord.DistanceReal(inputOnHorizontalLine).Value;
            //var xUv = xDistance / _realLifeMapSize.x;

            //var uv = new Vector2(xUv, yUv);

            //return RectangleUtils.CalculateSubPosition(_mapUnityCoords, uv);
        }

        public GeoCoordinate TranslateToGeo(Vector2 unityPosition)
        {
            var rootDelta = unityPosition - _rootPoint.UnityPosition;
            var deltaInGeoUnits = new Vector2(rootDelta.x * (1f / _geoUnitsInUnitySize.x),
                rootDelta.y * (1f / _geoUnitsInUnitySize.y));
            return (_rootPoint.GeoCoord.ToVector2() + deltaInGeoUnits).ToGeoCoordinate();
        }

        public static GeoCoordsToUnityTranslator DefaultTranslator = new GeoCoordsToUnityTranslator(
            new MyRectangle(0, 0, 80640, 80640),
            //new UnityCoordsPositions2D(0,0,  73171f, 111219f),
            new GeoCoordsRect(new GeoCoordinate(49, 19), new GeoCoordinate(50, 20)),
            new GeoCoordsTranslatorRootPoint()
            {
                UnityPosition = new Vector2(47548.6f, 52890.4f),
                GeoCoord = new GeoCoordinate(49.613, 19.5510)
            });
    }

    public class GeoCoordsTranslatorRootPoint
    {
        public Vector2 UnityPosition;
        public GeoCoordinate GeoCoord;
    }
}