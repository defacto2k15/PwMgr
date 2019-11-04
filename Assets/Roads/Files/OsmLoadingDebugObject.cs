using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Roads.Osm;
using Assets.Roads.Pathfinding;
using Assets.TerrainMat;
using Assets.Utils;
using NetTopologySuite.Geometries;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Roads.Files
{
    public class OsmLoadingDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            DebugTerrainTester tester = new DebugTerrainTester(ComputeShaderContainer);
            tester.Start();

            var _extractor = new OsmWaysExtractor();
            var osmPath = @"C:\inz\osm\map.osm";
            var osmWays = _extractor.ExtractWays(osmPath,
                    new GeoCoordsRect(new GeoCoordinate(0, 0), new GeoCoordinate(60, 60)))
                .ToList();

            var coordTranslator = GeoCoordsToUnityTranslator.DefaultTranslator;

            _paths = osmWays.Select(
                c => c.Nodes.Select(
                    k => coordTranslator.TranslateToUnity(k.Position) /*0.01875f*/).ToList()).ToList();

            var pp1 = _paths[0][0];
            var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.localPosition = new Vector3(pp1.x, 0, pp1.y);

            //var basePos = new IntVector2(7,8);
            //var startPosX = new List<IntVector2>()
            //{
            //    basePos + new IntVector2(0, 0),
            //    basePos + new IntVector2(0, 1),
            //    basePos + new IntVector2(1, 0),
            //    basePos + new IntVector2(1, 1),
            //};
            //float baseTerrainLength = 5760;
            //var resolution = TerrainCardinalResolution.MIN_RESOLUTION;

            var basePos = new IntVector2(524, 583);
            var startPosX = new List<IntVector2>()
            {
                basePos + new IntVector2(0, 0),
                basePos + new IntVector2(0, 1),
                basePos + new IntVector2(1, 0),
                basePos + new IntVector2(1, 1),
            };

            float baseTerrainLength = 90;
            var resolution = TerrainCardinalResolution.MAX_RESOLUTION;
            CreateTerrains(startPosX, tester.TerrainShapeDbProxy, baseTerrainLength, resolution);

            var fileManager = new PathFileManager();
            var loadedQuantasizedPaths = fileManager.LoadPaths(@"C:\inz\wrt\");
            _processedPaths = loadedQuantasizedPaths.Select(p => p.PathNodes).ToList();
        }

        private List<List<Vector2>> _paths;
        private List<List<Vector2>> _processedPaths;

        public void OnDrawGizmos()
        {
            if (_paths != null)
            {
                foreach (var path in _paths)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        var p1 = path[i];
                        var p2 = path[i + 1];

                        var pp1 = new Vector3(p1.x, 0, p1.y);
                        var pp2 = new Vector3(p2.x, 0, p2.y);

                        //var color = new Color( (i%10)/10f, (i%20)/20f, (i%15)/15f);
                        var color = new Color(1, 0, 0);
                        Gizmos.color = color;

                        Gizmos.DrawLine(pp1, pp2);
                    }
                }
            }

            if (_processedPaths != null)
            {
                foreach (var path in _processedPaths)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        var p1 = path[i];
                        var p2 = path[i + 1];

                        var pp1 = new Vector3(p1.x, 0.01f, p1.y);
                        var pp2 = new Vector3(p2.x, 0.01f, p2.y);

                        //var color = new Color( (i%10)/10f, (i%20)/20f, (i%15)/15f);
                        var color = new Color(0, 1, 0);
                        Gizmos.color = color;

                        Gizmos.DrawLine(pp1, pp2);
                    }
                }
            }
        }


        public void CreateTerrains(List<IntVector2> startPos, TerrainShapeDbProxy shapeDb, float baseTerrainLength,
            TerrainCardinalResolution terrainResolution)
        {
            foreach (var aPos in startPos)
            {
                var unityCoordsPositions2D = new MyRectangle(aPos.X * baseTerrainLength,
                    aPos.Y * baseTerrainLength, baseTerrainLength, baseTerrainLength);
                var heightTex = shapeDb.Query(new TerrainDescriptionQuery()
                    {
                        QueryArea = unityCoordsPositions2D,
                        RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                        {
                            new TerrainDescriptionQueryElementDetail()
                            {
                                Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                                Resolution = terrainResolution
                            }
                        }
                    }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement
                    .DetailElement.Texture.Texture;
                CreateTerrainObject(heightTex, new Vector2(unityCoordsPositions2D.X, unityCoordsPositions2D.Y),
                    baseTerrainLength);
            }
        }

        public static GameObject CreateTerrainObject(Texture heightTexture, Vector2 startPos, float baseTerrainLength)
        {
            var mesh = PlaneGenerator.CreateFlatPlaneMesh(241, 241);

            var terrainPos = startPos; //new Vector2(startPosNo.X * 100f, startPosNo.Y * 100);
            var terrainObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            terrainObject.GetComponent<MeshFilter>().mesh = mesh;
            terrainObject.transform.localScale = new Vector3(baseTerrainLength, 4000, baseTerrainLength);
            terrainObject.transform.position = new Vector3(terrainPos.x, -20f, terrainPos.y);
            //terrainObject.transform.SetParent(parentGo.transform);

            var material = new Material(Shader.Find("Custom/Terrain/TestTerrainDirectPlain"));
            material.SetTexture("_HeightmapTex", heightTexture);
            terrainObject.GetComponent<MeshRenderer>().material = material;
            terrainObject.name = "testTerrain " + startPos;

            return terrainObject;
        }
    }
}