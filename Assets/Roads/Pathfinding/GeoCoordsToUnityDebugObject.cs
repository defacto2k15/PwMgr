using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Osm;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Roads.Pathfinding
{
    public class GeoCoordsToUnityDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var translator = GeoCoordsToUnityTranslator.DefaultTranslator;

            var positionsToTest = new List<GeoCoordinate>()
            {
                new GeoCoordinate(49, 19),
                new GeoCoordinate(50, 19),
                new GeoCoordinate(49, 20),
                new GeoCoordinate(50, 20),
                //new GeoCoordinate(49.565f, 19.477f),
                //new GeoCoordinate(49.565f, 19.0f),
                //new GeoCoordinate(49.565f, 19.2f),
                //new GeoCoordinate(49.565f, 19.4f),
                //new GeoCoordinate(49.565f, 19.8f),
                new GeoCoordinate(49.613f, 19.6f),
                new GeoCoordinate(49.613f, 19.5510f),
            };

            foreach (var geoPos in positionsToTest)
            {
                Debug.Log($"T63: geoPos {geoPos} unity {translator.TranslateToUnity(geoPos)}");
            }

            var pos1 = translator.TranslateToUnity(new GeoCoordinate(49.6108832, 19.5435037));
            var pos2 = translator.TranslateToUnity(new GeoCoordinate(49.612561, 19.546638));

            Debug.Log($"P1 {pos1} P2 {pos2}, fin {pos2 - pos1} p3 {(pos2 - pos1).magnitude}");
        }
    }
}