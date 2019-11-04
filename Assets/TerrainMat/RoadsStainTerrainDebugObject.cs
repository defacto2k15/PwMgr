using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Files;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Quadtree;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using NetTopologySuite.Simplify;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class RoadsStainTerrainDebugObject : MonoBehaviour
    {
        public GameObject RenderTextureGameObject;
        private List<Vector2> _pathPoints;
        private List<List<Vector2>> _rectanglePoints;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var loader = new PathFileManager();
            _pathPoints = loader.LoadPaths(@"C:\inz\wrt\").First().PathNodes;

            var simplifier = new PathSimplifier(0.1f);
            _pathPoints = simplifier.Simplify(_pathPoints);

            GameObject.CreatePrimitive(PrimitiveType.Capsule).transform.localPosition =
                _pathPoints.Select(c => new Vector3(c.x, 0, c.y)).First();

            var conventer = new LineStringToBoxesPathConventer();

            var generatedPolygons =
                conventer.Convert(
                    new LineString(_pathPoints.Select(c => MyNetTopologySuiteUtils.ToCoordinate(c))
                        .ToArray()), 1f);

            _rectanglePoints = generatedPolygons.Boxes.Select(b => new List<Vector2>()
            {
                b.BottomPair[0],
                b.BottomPair[1],
                b.TopPair[1],
                b.TopPair[0],
            }).ToList();


            BiomeInstancesContainer container = new BiomeInstancesContainer(new BiomesContainerConfiguration()
            {
                Center = new Vector2(0.5f, 0.5f),
                DefaultType = BiomeType.Forest,
                HighQualityQueryDistance = 999999.1f
            });

            container.AddBiome(
                new PathBiomeInstanceInfo(generatedPolygons, BiomeType.Sand, 9, new BiomeInstanceId(312)));

            var width = (48609 - 48157);
            var height = 52243 - 51832;
            var arrayGenerator2 = new StainTerrainArrayFromBiomesGenerator(
                container,
                BiomeGenerationDebugObject.DebugDetailGenerator(),
                new StainSpaceToUnitySpaceTranslator(new MyRectangle(
                    48157, 51832, width, height
                )),
                new StainTerrainArrayFromBiomesGeneratorConfiguration()
                {
                    TexturesSideLength = 16
                });

            var resourceGenerator = new ComputationStainTerrainResourceGenerator(
                new StainTerrainResourceComposer(
                    new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator())),
                new StainTerrainArrayMelder(),
                arrayGenerator2);
            StainTerrainResource terrainResource = resourceGenerator.GenerateTerrainTextureDataAsync().Result;

            var newMaterial = new Material(Shader.Find("Custom/TerrainTextureTest2"));
            BiomeGenerationDebugObject.ConfigureMaterial(terrainResource, newMaterial);
            RenderTextureGameObject.GetComponent<MeshRenderer>().material = newMaterial;
        }

        public void OnDrawGizmos()
        {
            if (_pathPoints != null && _rectanglePoints != null)
            {
                Gizmos.color = new Color(1, 0, 0);
                foreach (var p in _pathPoints.AdjecentPairs())
                {
                    Gizmos.DrawLine(new Vector3(p.A.x, 0, p.A.y), new Vector3(p.B.x, 0, p.B.y));
                }
                ;

                int i = 0;
                foreach (var rect in _rectanglePoints)
                {
                    Gizmos.color = new Color(0, (i % 10) / 10f, 0);
                    foreach (var p in rect.AdjecentCirclePairs())
                    {
                        Gizmos.DrawLine(new Vector3(p.A.x, 0, p.A.y), new Vector3(p.B.x, 0, p.B.y));
                    }
                    i++;
                }
            }
        }
    }

    public class PathBiomeInstanceInfo : BiomeInstanceInfo
    {
        private MyQuadtree<OnePathBox> _pathBoxesTree = new MyQuadtree<OnePathBox>();
        private Envelope _envelope;

        public PathBiomeInstanceInfo(BoxesPath path,
            BiomeType type, int priority, BiomeInstanceId instanceId) : base(type, priority, instanceId)
        {
            foreach (var box in path.Boxes)
            {
                _pathBoxesTree.Add(box);
            }
            _envelope = MyNetTopologySuiteUtils.ToEnvelope(path.Boxes.SelectMany(c => new List<Vector2>
            {
                c.TopPair[0],
                c.TopPair[1],
                c.BottomPair[0],
                c.BottomPair[1]
            }).ToList());
        }

        public override Envelope CalculateEnvelope()
        {
            return _envelope;
        }

        public override bool Intersects(IGeometry geometry)
        {
            return _pathBoxesTree.Query(geometry.Envelope).Any(i => i.Intersects(geometry));
        }

        public override float IntersectionArea(IGeometry geometry)
        {
            var elementsInQueryGeometry = _pathBoxesTree.Query(geometry);
            float sum = 0;
            foreach (var element in elementsInQueryGeometry)
            {
                sum += (float) element.ToPolygon().Intersection(geometry).Area;
            }
            return sum;

            //var toReturn = (float) _pathBoxesTree.Query(geometry).Sum(c => c.ToPolygon().Intersection(geometry).Area);
            //return toReturn;
        }

        public override bool VisibleAtLowQuality()
        {
            return false;
        }
    }

    public class PathSimplifier
    {
        private double _tolerance;

        public PathSimplifier(double tolerance)
        {
            _tolerance = tolerance;
        }

        public ILineString Simplify(ILineString path)
        {
            var simplifier = new DouglasPeuckerLineSimplifier(path.Coordinates);
            simplifier.DistanceTolerance = _tolerance;
            return new LineString(simplifier.Simplify());
        }

        public List<Vector2> Simplify(List<Vector2> points)
        {
            var lineString = new LineString(points.Select(c => MyNetTopologySuiteUtils.ToCoordinate(c)).ToArray());
            var simplified = Simplify(lineString);
            return simplified.Coordinates.Select(c => MyNetTopologySuiteUtils.ToVector2(c)).ToList();
        }
    }

    public class LineStringToBoxesPathConventer
    {
        public BoxesPath Convert(ILineString path, float pathWidth)
        {
            var pathRadius = pathWidth / 2f;
            var points = path.Coordinates.Select(c => MyNetTopologySuiteUtils.ToVector2(c)).ToList();
            List<OnePathBox> rectBoxesList = new List<OnePathBox>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];

                var deltaVec = (p2 - p1).normalized;
                var perpendicularVec = new Vector2(deltaVec.y, -deltaVec.x).normalized;

                rectBoxesList.Add(new OnePathBox()
                {
                    BottomPair = new[]
                    {
                        p1 - perpendicularVec * pathRadius,
                        p1 + perpendicularVec * pathRadius,
                    },
                    TopPair = new[]
                    {
                        p2 - perpendicularVec * pathRadius,
                        p2 + perpendicularVec * pathRadius,
                    }
                });
            }


            var bottomPair = rectBoxesList[0].BottomPair;
            List<Vector2[]> pairs = new List<Vector2[]>();
            pairs.Add(bottomPair);
            for (int i = 0; i < rectBoxesList.Count - 1; i++)
            {
                var topBox = rectBoxesList[i];
                var botBox = rectBoxesList[i + 1];

                var pair = new Vector2[]
                {
                    VectorUtils.Average(topBox.BottomPair[0], botBox.TopPair[0]),
                    VectorUtils.Average(topBox.BottomPair[1], botBox.TopPair[1]),
                };
                pairs.Add(pair);
            }
            pairs.Add(rectBoxesList.Last().TopPair);

            var outList = pairs.AdjecentPairs().Select(p => new OnePathBox()
            {
                BottomPair = p.A,
                TopPair = p.B
            }).ToList();

            return new BoxesPath(outList);
        }
    }

    public class BoxesPath
    {
        private readonly List<OnePathBox> _boxes;

        public BoxesPath(List<OnePathBox> boxes)
        {
            _boxes = boxes;
        }

        public List<OnePathBox> Boxes => _boxes;
    }

    public class OnePathBox : IHasEnvelope, ICanTestIntersect
    {
        public Vector2[] BottomPair;
        public Vector2[] TopPair;

        public Envelope CalculateEnvelope()
        {
            return MyNetTopologySuiteUtils.ToEnvelope(new List<Vector2>()
            {
                BottomPair[0],
                BottomPair[1],
                TopPair[0],
                TopPair[1]
            });
        }

        public bool Intersects(IGeometry geometry)
        {
            var polygon = ToPolygon();
            return polygon.Intersects(geometry);
        }

        public IGeometry ToPolygon()
        {
            return MyNetTopologySuiteUtils.CreateConvexHullFromPoints(new Vector2[]
            {
                BottomPair[0],
                BottomPair[1],
                TopPair[0],
                TopPair[1]
            });
        }
    }
}