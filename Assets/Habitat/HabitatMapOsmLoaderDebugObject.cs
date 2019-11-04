using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.TerrainMat;
using MathNet.Numerics;
using NetTopologySuite.GeometriesGraph;
using NetTopologySuite.Operation.Valid;
using OsmSharp.Osm.Xml.v0_6;
using UnityEngine;

namespace Assets.Habitat
{
    class HabitatMapOsmLoaderDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var loader = new HabitatMapOsmLoader();
            var fields = loader.Load(@"C:\inz\osm\map.osm");

            foreach (var field in fields)
            {
                CreateDebugHabitatField(field);
            }
        }

        public static void CreateDebugHabitatField(HabitatField field, float positionMultiplier = 1000,
            GameObject parentGameObject = null)
        {
            var polys = MyNetTopologySuiteUtils.ToSinglePolygons(field.Geometry);
            if (polys.Count == 1)
            {
                var poly = polys[0];
                var exteriorRing = poly.ExteriorRing.Coordinates.Select(i => MyNetTopologySuiteUtils.ToVector2(i))
                    .ToList();

                var rootObject = CreateRingGameObject(exteriorRing, positionMultiplier);
                if (parentGameObject != null)
                {
                    rootObject.transform.SetParent(parentGameObject.transform);
                }
                rootObject.name = field.Type.ToString();


                foreach (var interiorRing in poly.Holes)
                {
                    var ring
                        = interiorRing.Coordinates.Select(i => MyNetTopologySuiteUtils.ToVector2(i)).ToList();
                    var ringObject = CreateRingGameObject(ring, positionMultiplier);
                    ringObject.transform.SetParent(rootObject.transform);
                }
            }
            else
            {
                foreach (var habitatField in polys.Select(poly => new HabitatField()
                {
                    Geometry = poly,
                    Type = field.Type
                }))
                {
                    CreateDebugHabitatField(habitatField, positionMultiplier, parentGameObject);
                }
            }
        }

        private static GameObject CreateRingGameObject(List<Vector2> nodes, float positionMultiplier)
        {
            var parent = new GameObject();
            foreach (var pos in nodes)
            {
                var nodeObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                nodeObject.transform.SetParent(parent.transform);
                nodeObject.transform.position = new Vector3(pos.x * positionMultiplier, 0, pos.y * positionMultiplier);
                nodeObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                //parent.transform.position = nodeObject.transform.position;
            }
            return parent;
        }
    }
}