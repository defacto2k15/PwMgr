using System;
using System.Text;
using Assets.Roads.Osm;
using Assets.Utils;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Structures.QTree;
using OsmSharp.Osm.PBF.Streams;
using UnityEngine;

namespace Assets.Roads
{
    public class RoadDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var extractor = new OsmWaysExtractor();
            var ways = extractor.ExtractWays(@"C:\inz\osm\map.osm",
                new GeoCoordsRect(
                    //new GeoCoordinate(49.5f,19.4f),
                    //new GeoCoordinate(49.6f,19.52f) ));
                    new GeoCoordinate(0, 0),
                    new GeoCoordinate(60, 60)));

            foreach (var way in ways)
            {
                var parent = new GameObject(way.ToString());
                foreach (var node in way.Nodes)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = ThreeDPositionOfNode(node);
                    cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    cube.transform.SetParent(parent.transform);
                }
            }
        }

        public static Vector3 ThreeDPositionOfNode(MyWorkNode workNode)
        {
            var pos2d = PositionOfNode(workNode);
            return new Vector3(pos2d.x, 0, pos2d.y);
        }

        private static Vector2 PositionOfNode(MyWorkNode workNode)
        {
            return workNode.Position * 100;
        }
    }
}