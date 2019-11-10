using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.Quadtree;
using GeoAPI.Geometries;
using NetTopologySuite.Operation.Union;
using UnityEngine;

namespace Assets.Habitat
{
    public class HabitatMap
    {
        private readonly Vector2 _mapStartPosition;
        private readonly MyQuadtree<HabitatFieldInTree>[,] _treeArray;
        private readonly Vector2 _mapGridSize;
        private readonly IntVector2 _gridCellsCount;

        public HabitatMap(
            Vector2 mapStartPosition,
            Vector2 mapGridSize,
            IntVector2 gridCellsCount,
            MyQuadtree<HabitatFieldInTree>[,] treeArray)
        {
            _mapStartPosition = mapStartPosition;
            _treeArray = treeArray;
            _mapGridSize = mapGridSize;
            _gridCellsCount = gridCellsCount;
        }

        public MyQuadtree<HabitatFieldInTree> QueryMap(MyRectangle queryArea)
        {
            var outTree = new MyQuadtree<HabitatFieldInTree>();

            Vector2 reoffsetStartPosition = new Vector2(queryArea.X, queryArea.Y) - _mapStartPosition;
            Vector2 reoffsetEndPosition = new Vector2(queryArea.X + queryArea.Width, queryArea.Y + queryArea.Height)
                                          - _mapStartPosition;

            var startGridX = Mathf.FloorToInt(reoffsetStartPosition.x / _mapGridSize.x);
            var startGridY = Mathf.FloorToInt(reoffsetStartPosition.y / _mapGridSize.y);

            var endGridX = Mathf.Min(_gridCellsCount.X, Mathf.CeilToInt(reoffsetEndPosition.x / _mapGridSize.x));
            var endGridY = Mathf.Min(_gridCellsCount.Y, Mathf.CeilToInt(reoffsetEndPosition.y / _mapGridSize.y));

            if (startGridX < 0 || startGridY < 0 || endGridX > _gridCellsCount.X || endGridY > _gridCellsCount.Y)
            {
                Preconditions.Fail(
                    $"Querying area {queryArea}, makes grids query: x:{startGridX} - {endGridX} y:{startGridY}-{endGridY} which is out of size {_gridCellsCount}");
            }

            var queryEnvelope = MyNetTopologySuiteUtils.ToGeometryEnvelope(queryArea);
            for (var gridX = startGridX; gridX < endGridX; gridX++)
            {
                for (var gridY = startGridY; gridY < endGridY; gridY++)
                {
                    //todo return other structure - one that uses gridded tree in array
                    List<HabitatFieldInTree> treeElements = null;

                    treeElements = _treeArray[gridX, gridY].QueryAll().Select(c =>
                            new
                            {
                                intersection = c.Field.Geometry.Intersection(queryEnvelope),
                                elem = c
                            })
                        .Where(c => !c.intersection.IsEmpty && !(c.intersection is ILineString))
                        .SelectMany(c => MyNetTopologySuiteUtils.ToSinglePolygons(c.intersection).Select(
                            k => new HabitatFieldInTree()
                            {
                                Field = new HabitatField()
                                {
                                    Geometry = k,
                                    Type = c.elem.Field.Type
                                }
                            })).ToList();

                    treeElements.ForEach(c => outTree.Add(c));
                }
            }

            return outTree;
        }

        public List<HabitatField> QueryAll()
        {
            var outList = new List<HabitatField>();
            for (int x = 0; x < _gridCellsCount.X; x++)
            {
                for (int y = 0; y < _gridCellsCount.Y; y++)
                {
                    outList.AddRange(_treeArray[x, y].QueryAll().Select(c => c.Field));
                }
            }
            return outList;
        }

        public Vector2 MapStartPosition => _mapStartPosition;
        public MyQuadtree<HabitatFieldInTree>[,] TreeArray => _treeArray;
        public Vector2 MapGridSize => _mapGridSize;
        public IntVector2 GridCellsCount => _gridCellsCount;


        public static HabitatMap Create(
            MyRectangle areaOnMap,
            Vector2 mapGridSize,
            List<HabitatField> habitatFields,
            HabitatType defaultHabitatType,
            HabitatTypePriorityResolver priorityResolver)
        {
            var fullTree = new MyQuadtree<HabitatFieldInTree>();
            habitatFields.ForEach(c => fullTree.Add(new HabitatFieldInTree()
            {
                Field = c
            }));

            int gridXCount = Mathf.CeilToInt(areaOnMap.Width / mapGridSize.x);
            int gridYCount = Mathf.CeilToInt(areaOnMap.Height / mapGridSize.y);

            var gridTreeArray = new MyQuadtree<HabitatFieldInTree>[gridXCount, gridYCount];

            for (int gridX = 0; gridX < gridXCount; gridX++)
            {
                for (int gridY = 0; gridY < gridYCount; gridY++)
                {
                    var gridArea = new MyRectangle(
                        areaOnMap.X + gridX * mapGridSize.x,
                        areaOnMap.Y + gridY * mapGridSize.y,
                        mapGridSize.x,
                        mapGridSize.y);

                    var geometryEnvelope = MyNetTopologySuiteUtils.ToGeometryEnvelope(gridArea);
                    var fieldsInArea = fullTree.QueryWithIntersection(geometryEnvelope);
                    //Debug.Log("U34: "+fieldsInArea.Count(c => c.Field.Type == HabitatType.Fell));
                    //if (fieldsInArea.Count(c => c.Field.Type == HabitatType.Fell) > 0 )
                    //{
                    //    Debug.Log($"J2: {StringUtils.ToString(fieldsInArea.Where(c => c.Field.Type == HabitatType.Fell).Select(c => c.Field.Geometry.AsText()))}");
                    //}

                    var fieldsInEnvelope = fieldsInArea.Select(c => new HabitatField()
                    {
                        Geometry = MySafeIntersection(c.Field.Geometry, geometryEnvelope),
                        Type = c.Field.Type
                    }).Where(c => c.Geometry != null && !c.Geometry.IsEmpty);

                    // removal of elements one on other

                    var sortedFields = fieldsInEnvelope.OrderByDescending(c => priorityResolver.Resolve(c.Type))
                        .ToList();
                    for (int i = 0; i < sortedFields.Count; i++)
                    {
                        var current = sortedFields[i];
                        for (int j = i + 1; j < sortedFields.Count; j++)
                        {
                            var other = sortedFields[j];
                            other.Geometry = other.Geometry.Difference(current.Geometry);
                        }
                    }
                    sortedFields = sortedFields.Where(c => !c.Geometry.IsEmpty).ToList();


                    var notUsedAreaInGrid = geometryEnvelope;
                    foreach (var cutGeometry in sortedFields.Select(c => c.Geometry))
                    {
                        notUsedAreaInGrid = notUsedAreaInGrid.Difference(cutGeometry);
                    }
                    if (!notUsedAreaInGrid.IsEmpty)
                    {
                        sortedFields.Add(
                            new HabitatField()
                            {
                                Type = defaultHabitatType,
                                Geometry = notUsedAreaInGrid
                            }
                        );
                    }

                    var singleFieldsInTree = sortedFields.SelectMany(c => MyNetTopologySuiteUtils
                        .ToSinglePolygons(c.Geometry).Select(poly => new HabitatField()
                        {
                            Geometry = poly,
                            Type = c.Type
                        })).Select(c => new HabitatFieldInTree()
                    {
                        Field = c
                    }).ToList();
                    gridTreeArray[gridX, gridY] = MyQuadtree<HabitatFieldInTree>.CreateWithElements(singleFieldsInTree);
                }
            }
            return new HabitatMap(
                new Vector2(areaOnMap.X, areaOnMap.Y),
                mapGridSize,
                new IntVector2(gridXCount, gridYCount),
                gridTreeArray);
        }

        private static IGeometry MySafeIntersection(IGeometry fieldGeometry, IGeometry geometryEnvelope)
        {
            IGeometry toReturn = null;
            try
            {
                toReturn = fieldGeometry.Intersection(geometryEnvelope);
                return toReturn;
            }
            catch (Exception e)
            {
                Debug.Log("Failure of geometry: " + fieldGeometry.ToString());
                return null;
            }
        }
    }

    public class HabitatTypePriorityResolver
    {
        private Dictionary<HabitatType, int> _priorityDict;

        public HabitatTypePriorityResolver(Dictionary<HabitatType, int> priorityDict)
        {
            _priorityDict = priorityDict;
        }

        public int Resolve(HabitatType type)
        {
            return _priorityDict[type];
        }

        public static HabitatTypePriorityResolver Default => new HabitatTypePriorityResolver(
            new Dictionary<HabitatType, int>()
            {
                {HabitatType.Fell, 6},
                {HabitatType.Forest, 2},
                {HabitatType.Grassland, 3},
                {HabitatType.Meadow, 4},
                {HabitatType.NotSpecified, 1},
                {HabitatType.Scrub, 5},
            });
    }
}