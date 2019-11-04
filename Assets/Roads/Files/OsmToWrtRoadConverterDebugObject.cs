using System;
using System.Text;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Osm;
using Assets.Roads.Pathfinding;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Roads.Files
{
    public class OsmToWrtRoadConverterDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;

        public void Start()
        {
            DebugTerrainTester terrainTester = new DebugTerrainTester(ContainerGameObject);
            terrainTester.Start();

            var converter = OsmToWrtRoadConverter.Create(
                new OsmToWrtRoadConverter.OsmToWrtConverterConfiguration(),
                terrainTester.TerrainShapeDbProxy,
                GeoCoordsToUnityTranslator.DefaultTranslator
            );

            converter.Convert(@"C:\inz\osm\map.osm", @"C:\inz\wrtC\",
                //new GeoCoordsRect(new GeoCoordinate(0, 0), new GeoCoordinate(60, 60)));
                new GeoCoordsRect(
                    new GeoCoordinate(49.601, 19.541412),
                    new GeoCoordinate(49.6183, 19.5628)));
        }
    }
}