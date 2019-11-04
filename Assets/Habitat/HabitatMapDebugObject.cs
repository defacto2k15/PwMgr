using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Pathfinding;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Habitat
{
    public class HabitatMapDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Loading");
            var loader = new HabitatMapOsmLoader();
            var fields = loader.Load(@"C:\inz\osm\map.osm");

            msw.StartSegment("Translating");
            var translator = new HabitatFieldPositionTranslator(GeoCoordsToUnityTranslator.DefaultTranslator);
            fields = fields.Select(c => translator.Translate(c)).ToList();

            msw.StartSegment("MapCreating");
            var map = HabitatMap.Create(
                new MyRectangle(62 * 720, 67 * 720, 8 * 720, 8 * 720),
                new Vector2(90 * 8, 90 * 8),
                fields,
                HabitatType.NotSpecified,
                HabitatTypePriorityResolver.Default);


            GeoCoordsToUnityTranslator translator2 = GeoCoordsToUnityTranslator.DefaultTranslator;
            var geoPos1 = new GeoCoordinate(49.600f, 19.543f);
            var lifePos1 = translator2.TranslateToUnity(geoPos1);
            var geoPos2 = new GeoCoordinate(49.610f, 19.552f);
            var lifePos2 = translator2.TranslateToUnity(geoPos2);
            MyRectangle territoryUsedToCreateStainTexture =
                new MyRectangle(lifePos1.x, lifePos1.y, lifePos2.x - lifePos1.x, lifePos2.y - lifePos1.y);
            territoryUsedToCreateStainTexture = RectangleUtils.CalculateSubPosition(territoryUsedToCreateStainTexture,
                new MyRectangle(0.65f, 0.65f, 0.1f, 0.1f));

            var treeX = map.QueryMap(territoryUsedToCreateStainTexture);
            var parentObjectX = new GameObject($"MAGA");
            treeX.QueryAll()
                .ForEach(c => HabitatMapOsmLoaderDebugObject.CreateDebugHabitatField(c.Field, 0.01f, parentObjectX));

            msw.StartSegment("MapQuerying");
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var tree = map.QueryMap(
                        new MyRectangle(62 * 720 + x * 720, 67 * 720 + y * 720, 720, 720));
                    var parentObject = new GameObject($"x:{x} y:{y}");
                    tree.QueryAll()
                        .ForEach(c => HabitatMapOsmLoaderDebugObject.CreateDebugHabitatField(c.Field, 0.01f,
                            parentObject));
                }
            }
            Debug.Log("T64: " + msw.CollectResults());
        }
    }
}