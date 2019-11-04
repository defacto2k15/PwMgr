using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Roads.Engraving;
using Assets.Roads.Pathfinding.Fitting;
using Assets.Roads.Pathfinding.TerrainPath;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Roads.Pathfinding
{
    public class GeneralPathCreatorDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;
        public Texture OutTexture;

        public void Start()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("StartCreating");
            DebugTerrainTester debugTerrainTester = new DebugTerrainTester(ContainerGameObject);
            debugTerrainTester.Start();

            TerrainPathfinder terrainPathfinder =
                new TerrainPathfinder(
                    new TerrainPathfinder.TerrainPathfinderConfiguration(), 1,
                    debugTerrainTester.TerrainShapeDbProxy,
                    null, null);

            GratedPathSimplifier gratedPathSimplifier = new GratedPathSimplifier();
            PathFitter pathFitter = new PathFitter();

            var samplesPerUnit = 4f;
            PathQuantisizer pathQuantisizer = new PathQuantisizer(samplesPerUnit);
            var pathCreator = new GeneralPathCreator(terrainPathfinder, gratedPathSimplifier, pathFitter,
                pathQuantisizer);

            var nodes = new List<List<Vector2>>()
            {
                new List<Vector2>
                {
                    new Vector2(50, 50),
                    new Vector2(100, 50),
                    new Vector2(50, 180),
                    new Vector2(120, 120),
                    new Vector2(120, 180),
                },
                new List<Vector2>
                {
                    new Vector2(30, 10),
                    new Vector2(150, 70),
                    new Vector2(50, 180),
                    new Vector2(10, 120),
                    new Vector2(200, 10),
                },
            };


            msw.StartSegment("GeneratePath");
            var createdPaths = nodes.Select(n => pathCreator.GeneratePath(n)).ToList();

            msw.StartSegment("GenerateproximityArray");

            var maximumProximity = 5f;
            var maxDelta = 10f;

            var proximityArrayGenerator = new PathProximityArrayGenerator(
                new PathProximityArrayGenerator.PathProximityArrayGeneratorConfiguration()
                {
                    MaximumProximity = maximumProximity,
                    ArraySize = new IntVector2(241, 241)
                });

            var proximityTextureGenerator = new PathProximityTextureGenerator(new TextureConcieverUTProxy(),
                new PathProximityTextureGenerator.PathProximityTextureGeneratorConfiguration()
                {
                    MaximumProximity = maximumProximity,
                    MaxDelta = maxDelta
                });

            foreach (var terrainGridPos in _terrainObjectGridPosList)
            {
                var terrainCoords = new MyRectangle(terrainGridPos.X * 90, terrainGridPos.Y * 90, 90, 90);

                var pathProximityArray =
                    proximityArrayGenerator.Generate(createdPaths.ToList(), terrainCoords);

                msw.StartSegment("GenerateProximityTexture");

                var pathProximityTexture = proximityTextureGenerator.GeneratePathProximityTexture(pathProximityArray)
                    .Result;

                var heightTexture = debugTerrainTester.TerrainShapeDbProxy.Query(new TerrainDescriptionQuery()
                {
                    QueryArea = terrainCoords,
                    RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                    {
                        new TerrainDescriptionQueryElementDetail()
                        {
                            Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY,
                            Resolution = TerrainCardinalResolution.MAX_RESOLUTION
                        }
                    }
                }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);

                SavingFileManager.SaveTextureToPngFile(@"C:\inz\tmp\p2.png", pathProximityTexture.Texture as Texture2D);

                _sketchTerrainTexturesDict[terrainGridPos] = new SketchTerrainTextures()
                {
                    HeightTexture = heightTexture.TokenizedElement.DetailElement.Texture.Texture,
                    PathProximityTexture = pathProximityTexture.Texture
                };
            }

            var roadEngraverConfiguration = new RoadEngraver.RoadEngraverConfiguration()
            {
                StartSlopeProximity = 0.75f,
                EndSlopeProximity = 3,
                MaxDelta = maxDelta,
                MaxProximity = maximumProximity
            };
            _roadEngraver = new RoadEngraver(ContainerGameObject, new UnityThreadComputeShaderExecutorObject(),
                roadEngraverConfiguration);

            RecreateTerrain();

            Debug.Log($"T51 {msw.CollectResults()}");
        }

        public static GameObject CreateTerrainObject(Texture heightTexture, IntVector2 startPosNo)
        {
            var mesh = PlaneGenerator.CreateFlatPlaneMesh(241, 241);

            var terrainPos = new Vector2(startPosNo.X * 100f, startPosNo.Y * 100);
            var startPos = new Vector2(startPosNo.X * 90f, startPosNo.Y * 90f);
            var terrainObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            terrainObject.GetComponent<MeshFilter>().mesh = mesh;
            terrainObject.transform.localScale = new Vector3(100, 4000, 100);
            terrainObject.transform.position = new Vector3(terrainPos.x, -107.5f, terrainPos.y);
            //terrainObject.transform.SetParent(parentGo.transform);

            var material = new Material(Shader.Find("Custom/Terrain/TestTerrainDirectPlain"));
            material.SetTexture("_HeightmapTex", heightTexture);
            terrainObject.GetComponent<MeshRenderer>().material = material;
            terrainObject.name = "testTerrain " + startPos;

            return terrainObject;
        }

        public void RecreateTerrain()
        {
            _terrainGameObjects.ForEach(c => Destroy(c));
            _terrainGameObjects.Clear();

            foreach (var aGridPos in _terrainObjectGridPosList)
            {
                var sketchTextures = _sketchTerrainTexturesDict[aGridPos];

                var terrainCoords = new MyRectangle(aGridPos.X * 90, aGridPos.Y * 90, 90, 90);
                var outHeighTexture = _roadEngraver.EngraveRoads(
                    terrainCoords,
                    new IntVector2(241, 241),
                    new UvdSizedTexture()
                    {
                        TextureWithSize = new TextureWithSize()
                        {
                            Size = new IntVector2(241, 241),
                            Texture = sketchTextures.PathProximityTexture
                        },
                        Uv = new MyRectangle(0, 0, 1, 1)
                    },
                    sketchTextures.HeightTexture);
                var terrainObject = CreateTerrainObject(outHeighTexture.Result, aGridPos);
                _terrainGameObjects.Add(terrainObject);
            }
        }

        private RoadEngraver _roadEngraver;
        private List<Vector2> _samples = new List<Vector2>();

        private Dictionary<IntVector2, SketchTerrainTextures> _sketchTerrainTexturesDict =
            new Dictionary<IntVector2, SketchTerrainTextures>();

        private List<GameObject> _terrainGameObjects = new List<GameObject>();

        private List<IntVector2> _terrainObjectGridPosList = new List<IntVector2>
        {
            new IntVector2(0, 0),
            //new IntVector2(0, 1),
            //new IntVector2(1, 0),
            //new IntVector2(1, 1),
        };

        private class SketchTerrainTextures
        {
            public Texture PathProximityTexture;
            public Texture HeightTexture;
        }


        public void OnDrawGizmosSelected()
        {
            if (_samples != null)
            {
                Gizmos.color = Color.white;

                for (int i = 0; i < _samples.Count - 1; i++)
                {
                    var p1 = _samples[i];
                    var p2 = _samples[i + 1];

                    Gizmos.DrawLine(new Vector3(p1.x, p1.y, 0), new Vector3(p2.x, p2.y, 0));
                }
            }
        }
    }
}