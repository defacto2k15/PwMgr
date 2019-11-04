using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass2.IntensitySampling;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using Assets.Random.Fields;
using Assets.Ring2;
using Assets.TerrainMat;
using Assets.TerrainMat.BiomeGen;
using Assets.Trees.DesignBodyDetails;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Grass2
{
    public class PoissonDiskSamplingDebugObject : MonoBehaviour
    {
        public GameObject DebugTextureObject;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var generationArea = new MyRectangle(0, 0, 100, 100);
            var generationCount = 7;
            var exRadius = 2;

            var msw = new MyStopWatch();

            var randomFigureGenerator = new RandomFieldFigureGeneratorUTProxy(
                new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                    new Ring2RandomFieldFigureGeneratorConfiguration()
                    {
                        PixelsPerUnit = new Vector2(1, 1)
                    }));
            var randomFigure = randomFigureGenerator.GenerateRandomFieldFigureAsync(
                RandomFieldNature.FractalSimpleValueNoise3, 312,
                new MyRectangle(0, 0, 30, 30)).Result;

            //var intensityFigureProvider =  new IntensityFromRandomFigureProvider(randomFigure);
            //var intensityFigureProvider =  new IntensityFromRandomFigureProvider(CreateDebugRandomFieldFigure());
            var intensityFigureProvider = new IntensityFromRandomFiguresCompositionProvider(
                CreateDebugRandomFieldFigure(),
                randomFigure, 0.3f);

            msw.StartSegment("StartGeneration");

            var sampler = new MultiIntensityPoissonDiskSampler();
            var generatedPoints = sampler.Generate(generationArea, generationCount, new MyRange(0.5f, 3),
                new IntensityFromRandomFigureProvider(new IntensityFieldFigureWithUv()
                {
                    FieldFigure = randomFigure,
                    Uv = new MyRectangle(0, 0, 1, 1)
                }), 100000);

            //var noCollision = new NoCollisionMultiIntensityPoissonDiskSampler();
            //var generatedPoints = noCollision.Generate(generationArea, generationCount, 2,80000, 6, intensityFigureProvider);


            //var classicPoisson = new PoissonDiskSampler();
            //var generatedPoints = classicPoisson.Generate(generationArea, generationCount, exRadius);


            //var simpleCollidable = new SimpleRandomSampler();
            //var generatedPoints = simpleCollidable.Generate(generationArea, 15000, 40000, 0.1f, false, intensityFigureProvider);


            Debug.Log($"T{generatedPoints.Count} generation took: " + msw.CollectResults());

            var root = new GameObject("RootPoints");
            foreach (var point in generatedPoints)
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.transform.position = new Vector3(point.x, 0, point.y);
                cap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                cap.transform.SetParent(root.transform);
            }

            var newMaterial = new Material(Shader.Find("Standard"));
            newMaterial.SetTexture("_MainTex", Ring2RandomFieldFigureGenerator.DebugLastGeneratedTexture);
            DebugTextureObject.GetComponent<MeshRenderer>().material = newMaterial;
        }


        public static IntensityFieldFigure CreateDebugRandomFieldFigure()
        {
            int width = 16;
            int height = 16;

            var fieldFigure = new IntensityFieldFigure(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    fieldFigure.SetPixel(x, y, x / (float) (width - 1));
                }
            }
            return fieldFigure;
        }
    }

    public class SimpleRandomSampler
    {
        public List<Vector2> Generate(MyRectangle generationArea, int generationCount, float maxTries,
            float exclusionRadius, bool collisionsAreChecked, IIntensitySamplingProvider intensitySamplingProvider)
        {
            var random = new UnsafeRandomProvider(); //todo!
            var offsetGenerationArea = new MyRectangle(0, 0, generationArea.Width, generationArea.Height);

            float cellSize = exclusionRadius / Mathf.Sqrt(2);

            var width = generationArea.Width;
            var height = generationArea.Height;

            var grid = new SingleElementGenerationGrid(
                cellSize,
                Mathf.CeilToInt(width / cellSize), Mathf.CeilToInt(height / cellSize),
                exclusionRadius
            );

            var acceptedPoints = new List<Vector2>(generationCount);

            int generatedPointsCount = 0;
            int triesCount = 0;
            while (generatedPointsCount < generationCount && triesCount < maxTries)
            {
                var randomPoint = new Vector2(random.Next(0, width), random.Next(0, height));

                var randomPointValue = random.NextValue;
                var intensity =
                    intensitySamplingProvider.Sample(
                        RectangleUtils.CalculateSubelementUv(offsetGenerationArea, randomPoint));
                if (intensity >= randomPointValue)
                {
                    if (!collisionsAreChecked || !grid.Collides(randomPoint))
                    {
                        if (collisionsAreChecked)
                        {
                            grid.Set(randomPoint);
                        }

                        acceptedPoints.Add(randomPoint);
                        generatedPointsCount++;
                    }
                }

                triesCount++;
            }
            return acceptedPoints.Select(c => c + generationArea.DownLeftPoint).ToList();
        }
    }


    public class
        MultiIntensityPoissonDiskSampler //bazując na http://www.redblobgames.com/articles/noise/introduction.html
    {
        public int usedCount = 0;

        public List<Vector2> Generate(MyRectangle generationArea, float generationCount,
            MyRange exclusionRadiusRange,
            IIntensitySamplingProvider intensityProvider, int maxTries)

        {
            var random = new UnsafeRandomProvider(); //todo!
            var offsetGenerationArea = new MyRectangle(0, 0, generationArea.Width, generationArea.Height);

            float cellSize = exclusionRadiusRange.Max / Mathf.Sqrt(2);

            var width = generationArea.Width;
            var height = generationArea.Height;

            var grid = new GenerationMultipointGrid(
                cellSize,
                new IntVector2(Mathf.CeilToInt(width / cellSize), Mathf.CeilToInt(height / cellSize))
            );
            //if (usedCount++ % 2 == 0)
            //{
            //    return new List<Vector2>();
            //}

            var processList = new GenerationRandomQueue<PointWithExclusionRadius>();
            var acceptedPoints = new List<Vector2>(1000);


            PointWithExclusionRadius firstPoint = null;
            while (firstPoint == null)
            {
                maxTries--;
                if (maxTries < 0)
                {
                    return acceptedPoints;
                }

                var randX = random.Next(0, width);
                var randY = random.Next(0, height);
                var randomPoint = new Vector2(randY, randX);

                var exclusionRadius = CalculateExclusionRadius(randomPoint, offsetGenerationArea, exclusionRadiusRange,
                    intensityProvider);
                if (!exclusionRadius.HasValue)
                {
                    continue;
                }
                firstPoint = new PointWithExclusionRadius()
                {
                    ExclusionRadius = exclusionRadius.Value,
                    Point = randomPoint
                };
            }
            ;
            processList.Add(firstPoint);
            acceptedPoints.Add(firstPoint.Point);

            grid.Set(firstPoint);

            while (!processList.Empty)
            {
                var point = processList.RandomPop();
                for (int i = 0; i < generationCount; i++)
                {
                    Vector2 newPoint = GenerateRandomPointAround(point.Point, point.ExclusionRadius, random);
                    if (offsetGenerationArea.Contains(newPoint))
                    {
                        var calculatedExclusionRadius = CalculateExclusionRadius(newPoint, offsetGenerationArea,
                            exclusionRadiusRange, intensityProvider);
                        if (!calculatedExclusionRadius.HasValue)
                        {
                            continue;
                        }
                        var newPointWithExclusionRadius = new PointWithExclusionRadius()
                        {
                            Point = newPoint,
                            ExclusionRadius = calculatedExclusionRadius.Value
                        };

                        if (!grid.Collides(newPointWithExclusionRadius))
                        {
                            processList.Add(newPointWithExclusionRadius);
                            acceptedPoints.Add(newPoint);
                            grid.Set(newPointWithExclusionRadius);
                        }
                    }
                    maxTries--;
                }
                if (maxTries < 0)
                {
                    break;
                }
            }

            return acceptedPoints.Select(c => c + generationArea.DownLeftPoint).ToList();
        }

        private float? CalculateExclusionRadius(
            Vector2 point,
            MyRectangle offsetGenerationArea,
            MyRange exclusionRadiusRange,
            IIntensitySamplingProvider intensityProvider)
        {
            var uv = RectangleUtils.CalculateSubelementUv(offsetGenerationArea, point);
            var sample = intensityProvider.Sample(uv);

            var exclusionRadius = exclusionRadiusRange.Lerp(1 - sample);
            if (sample > 0.01f)
            {
                return exclusionRadius;
            }
            else
            {
                return null;
            }
        }

        private Vector2 GenerateRandomPointAround(Vector2 point, float exclusionRadius, UnsafeRandomProvider random)
        {
            var radius = exclusionRadius * random.Next(0, 2);
            var angle = random.Next(0, 2 * Mathf.PI);

            return new Vector2(
                point.x + radius * Mathf.Cos(angle),
                point.y + radius * Mathf.Sin(angle)
            );
        }
    }


    public class
        NoCollisionMultiIntensityPoissonDiskSampler
        //bazując na http://www.redblobgames.com/articles/noise/introduction.html
    {
        public List<Vector2> Generate(MyRectangle generationArea, float generationCount,
            float exclusionRadius, int maxTries, int maxPerGridCount, IIntensitySamplingProvider intensityProvider)
        {
            var random = new UnsafeRandomProvider(); //todo!
            var offsetGenerationArea = new MyRectangle(0, 0, generationArea.Width, generationArea.Height);

            float cellSize = exclusionRadius;

            var width = generationArea.Width;
            var height = generationArea.Height;

            var grid = new CountingGenerationGrid(
                cellSize,
                new IntVector2(Mathf.CeilToInt(width / cellSize), Mathf.CeilToInt(height / cellSize))
            );

            var processList = new GenerationRandomQueue<Vector2>();
            var acceptedPoints = new List<Vector2>(1000);

            var firstPoint = new Vector2(random.Next(0, width), random.Next(0, height));

            processList.Add(firstPoint);
            acceptedPoints.Add(firstPoint);
            grid.Increment(firstPoint);


            int tryCount = 0;

            while (!processList.Empty)
            {
                var point = processList.RandomPop();
                for (int i = 0; i < generationCount; i++)
                {
                    Vector2 newPoint = GenerateRandomPointAround(point, exclusionRadius, random);

                    if (offsetGenerationArea.Contains(newPoint))
                    {
                        var maxPointsInGrid =
                            intensityProvider.Sample(
                                RectangleUtils.CalculateSubelementUv(offsetGenerationArea, newPoint)) *
                            maxPerGridCount;
                        if (grid.Retrive(newPoint) < maxPointsInGrid)
                        {
                            processList.Add(newPoint);
                            acceptedPoints.Add(newPoint);
                            grid.Increment(newPoint);
                        }
                    }
                    tryCount++;
                }
                if (tryCount > maxTries)
                {
                    break;
                }
            }
            return acceptedPoints.Select(c => c + generationArea.DownLeftPoint).ToList();
        }

        private Vector2 GenerateRandomPointAround(Vector2 point, float exclusionRadius, UnsafeRandomProvider random)
        {
            var radius = exclusionRadius * random.Next(0, 2);
            var angle = random.Next(0, 2 * Mathf.PI);

            return new Vector2(
                point.x + radius * Mathf.Cos(angle),
                point.y + radius * Mathf.Sin(angle)
            );
        }
    }


    public class GenerationRandomQueue<T>
    {
        private List<T> _list = new List<T>();
        private UnsafeRandomProvider _randomProvider = new UnsafeRandomProvider(); //todo!

        public void Add(T newElem)
        {
            _list.Add(newElem);
        }

        public T RandomPop()
        {
            var randomIdx = _randomProvider.NextWithMax(0, _list.Count - 1);
            var elementToReturn = _list[randomIdx];
            _list.RemoveAt(randomIdx);
            return elementToReturn;
        }

        public bool Empty => !_list.Any();
    }


    public class PoissonDiskSampler //bazując na http://www.redblobgames.com/articles/noise/introduction.html
    {
        public List<Vector2> Generate(MyRectangle generationArea, float generationCount,
            float exclusionRadius)
        {
            var random = new UnsafeRandomProvider(); //todo!
            var offsetGenerationArea = new MyRectangle(0, 0, generationArea.Width, generationArea.Height);

            float cellSize = exclusionRadius / Mathf.Sqrt(2);

            var width = generationArea.Width;
            var height = generationArea.Height;

            var grid = new SingleElementGenerationGrid(
                cellSize,
                Mathf.CeilToInt(width / cellSize), Mathf.CeilToInt(height / cellSize),
                exclusionRadius
            );

            var processList = new GenerationRandomQueue<Vector2>();
            var acceptedPoints = new List<Vector2>(1000);

            var firstPoint = new Vector2(random.Next(0, width), random.Next(0, height));

            processList.Add(firstPoint);
            acceptedPoints.Add(firstPoint);

            grid.Set(firstPoint);

            while (!processList.Empty)
            {
                var point = processList.RandomPop();
                for (int i = 0; i < generationCount; i++)
                {
                    Vector2 newPoint = GenerateRandomPointAround(point, exclusionRadius, random);
                    if (offsetGenerationArea.Contains(newPoint) && !grid.Collides(newPoint))
                    {
                        processList.Add(newPoint);
                        acceptedPoints.Add(newPoint);
                        grid.Set(newPoint);
                    }
                }
            }
            return acceptedPoints.Select(c => c + generationArea.DownLeftPoint).ToList();
        }

        private Vector2 GenerateRandomPointAround(Vector2 point, float exclusionRadius, UnsafeRandomProvider random)
        {
            var radius = exclusionRadius * random.Next(0, 2);
            var angle = random.Next(0, 2 * Mathf.PI);

            return new Vector2(
                point.x + radius * Mathf.Cos(angle),
                point.y + radius * Mathf.Sin(angle)
            );
        }
    }


    public class CountingGenerationGrid
    {
        private readonly float _cellSize;
        private int[,] _array;

        public CountingGenerationGrid(float cellSize, IntVector2 arraySize)
        {
            _cellSize = cellSize;
            _array = new int[arraySize.X, arraySize.Y];
        }

        public void Increment(Vector2 point)
        {
            var pos = FindCellPosition(point);
            _array[pos.X, pos.Y]++;
        }

        public int Retrive(Vector2 point)
        {
            var pos = FindCellPosition(point);
            return _array[pos.X, pos.Y];
        }

        private IntVector2 FindCellPosition(Vector2 point)
        {
            return new IntVector2((int) (point.x / _cellSize), (int) (point.y / _cellSize));
        }


        private IEnumerable<IntVector2> FindNeighbourGridPositions(IntVector2 gridPoint)
        {
            var arrayWidth = _array.GetLength(0);
            var arrayHeight = _array.GetLength(1);
            var outList = new List<IntVector2>(GenerationGridUtils.NeighbourhoodOffsets.Count);
            foreach (var initialNeighbour in GenerationGridUtils.NeighbourhoodOffsets)
            {
                var c = initialNeighbour + gridPoint;
                if (c.X >= 0 && c.Y >= 0 && c.X < arrayWidth && c.Y < arrayHeight)
                {
                    outList.Add(c);
                }
            }
            return outList;
        }

        public override string ToString()
        {
            return $"Grid: {nameof(_cellSize)}: {_cellSize}";
        }
    }


    public class SingleElementGenerationGrid
    {
        private readonly float _cellSize;
        private Vector2?[,] _array;
        private float _exclusionRadius;

        public SingleElementGenerationGrid(float cellSize, int width, int height, float exclusionRadius)
        {
            _cellSize = cellSize;
            _exclusionRadius = exclusionRadius;
            _array = new Vector2?[width, height];
        }

        public bool IsCellFilled(Vector2 point)
        {
            var pos = FindCellPosition(point);
            return _array[pos.X, pos.Y].HasValue;
        }

        public void Set(Vector2 point)
        {
            var pos = FindCellPosition(point);
            Preconditions.Assert(!_array[pos.X, pos.Y].HasValue,
                $"Grid cell of index {pos.X} {pos.Y} arleady has value!");
            _array[pos.X, pos.Y] = point;
        }

        private IntVector2 FindCellPosition(Vector2 point)
        {
            return new IntVector2((int) (point.x / _cellSize), (int) (point.y / _cellSize));
        }

        public bool Collides(Vector2 point)
        {
            var gridPoint = FindCellPosition(point);

            var neighboursPos = FindNeighbourGridPositions(gridPoint);
            foreach (var cellPos in neighboursPos)
            {
                var neighbour = _array[cellPos.X, cellPos.Y];
                if (neighbour.HasValue)
                {
                    if (Vector2.Distance(neighbour.Value, point) < _exclusionRadius)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private IEnumerable<IntVector2> FindNeighbourGridPositions(IntVector2 gridPoint)
        {
            var arrayWidth = _array.GetLength(0);
            var arrayHeight = _array.GetLength(1);
            var outList = new List<IntVector2>(GenerationGridUtils.NeighbourhoodOffsets.Count);
            foreach (var initialNeighbour in GenerationGridUtils.NeighbourhoodOffsets)
            {
                var c = initialNeighbour + gridPoint;
                if (c.X >= 0 && c.Y >= 0 && c.X < arrayWidth && c.Y < arrayHeight)
                {
                    outList.Add(c);
                }
            }
            return outList;
        }
    }

    public class GenerationMultipointGrid
    {
        private readonly float _cellSize;
        private List<PointWithExclusionRadius>[,] _array;

        public GenerationMultipointGrid(
            float cellSize,
            IntVector2 gridSize)
        {
            _cellSize = cellSize;
            _array = new List<PointWithExclusionRadius>[gridSize.X, gridSize.Y];
        }

        public void Set(PointWithExclusionRadius pointWithExclusionRadius)
        {
            var point = pointWithExclusionRadius.Point;
            var exclusionRadius = pointWithExclusionRadius.ExclusionRadius;

            var pos = FindCellPosition(point);
            if (_array[pos.X, pos.Y] == null)
            {
                _array[pos.X, pos.Y] = new List<PointWithExclusionRadius>();
            }
            _array[pos.X, pos.Y].Add(new PointWithExclusionRadius()
            {
                Point = point,
                ExclusionRadius = exclusionRadius
            });
        }

        private IntVector2 FindCellPosition(Vector2 point)
        {
            return new IntVector2((int) (point.x / _cellSize), (int) (point.y / _cellSize));
        }

        public bool Collides(PointWithExclusionRadius pointWithExclusionRadius)
        {
            var point = pointWithExclusionRadius.Point;
            var exclusionRadius = pointWithExclusionRadius.ExclusionRadius;

            var gridPoint = FindCellPosition(point);

            var neighboursPos = FindNeighbourGridPositions(gridPoint);
            foreach (var cellPos in neighboursPos)
            {
                var neighboursInCell = _array[cellPos.X, cellPos.Y];
                if (neighboursInCell != null &&
                    neighboursInCell.Any(c => Vector2.Distance(c.Point, point) < exclusionRadius))
                {
                    return true;
                }
            }
            return false;
        }


        private IEnumerable<IntVector2> FindNeighbourGridPositions(IntVector2 gridPoint)
        {
            var arrayWidth = _array.GetLength(0);
            var arrayHeight = _array.GetLength(1);
            var outList = new List<IntVector2>(GenerationGridUtils.NeighbourhoodOffsets.Count);
            foreach (var initialNeighbour in GenerationGridUtils.NeighbourhoodOffsets)
            {
                var c = initialNeighbour + gridPoint;
                if (c.X >= 0 && c.Y >= 0 && c.X < arrayWidth && c.Y < arrayHeight)
                {
                    outList.Add(c);
                }
            }
            return outList;
        }

        public override string ToString()
        {
            return
                $"multipointGrid: {nameof(_cellSize)}: {_cellSize}, array size {_array.GetLength(0)}-{_array.GetLength(1)}";
        }
    }

    public static class GenerationGridUtils
    {
        public static List<IntVector2> NeighbourhoodOffsets = new List<IntVector2>()
        {
            new IntVector2(-2, -1),
            new IntVector2(-2, 0),
            new IntVector2(-2, 1),

            new IntVector2(-1, -2),
            new IntVector2(-1, -1),
            new IntVector2(-1, 0),
            new IntVector2(-1, 1),
            new IntVector2(-1, 2),

            new IntVector2(0, -2),
            new IntVector2(0, -1),
            new IntVector2(0, 0),
            new IntVector2(0, 1),
            new IntVector2(0, 2),

            new IntVector2(1, -2),
            new IntVector2(1, -1),
            new IntVector2(1, 0),
            new IntVector2(1, 1),
            new IntVector2(1, 2),

            new IntVector2(2, -1),
            new IntVector2(2, 0),
            new IntVector2(2, 1),
        };
    }

    public class PointWithExclusionRadius
    {
        public Vector2 Point;
        public float ExclusionRadius;
    }
}