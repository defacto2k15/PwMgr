using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Utils;
using Assets.Utils.Quadtree;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using UnityEngine;

namespace Assets.Habitat
{
    public class HabitatMapFileManager
    {
        public void SaveHabitatMap(string rootFilePath, HabitatMap map)
        {
            var pathGenerator = new HabitatMapFileManagerPathsGenerator(rootFilePath);

            var infoJson = HabitatMapInfoJson.Create(map);
            File.WriteAllText(pathGenerator.MainInfoFilePath(), JsonUtility.ToJson(infoJson));

            var wktWriter = new WKTWriter();
            var array = map.TreeArray;
            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    var tree = array[x, y];
                    var allElements = tree.QueryAll();

                    var fieldsInfo = new HabitatFieldsTypeInfoJson()
                    {
                        TypesList = allElements.Select(c => c.Field.Type).ToList()
                    };
                    File.WriteAllText(pathGenerator.GridInfoFile(x, y), JsonUtility.ToJson(fieldsInfo));

                    var oneCellFilePath = pathGenerator.GridWrtFile(x, y);
                    var geoCollection = new GeometryCollection(allElements.Select(c => c.Field.Geometry).ToArray());
                    File.WriteAllText(oneCellFilePath, wktWriter.Write(geoCollection));
                }
            }
        }

        public HabitatMap LoadHabitatMap(string rootFilePath)
        {
            var pathGenerator = new HabitatMapFileManagerPathsGenerator(rootFilePath);
            var infoJson = JsonUtility.FromJson<HabitatMapInfoJson>(File.ReadAllText(pathGenerator.MainInfoFilePath()));

            var gridCellsCount = infoJson.GridCellsCount;
            var treesArray = new MyQuadtree<HabitatFieldInTree>[gridCellsCount.X, gridCellsCount.Y];
            var wktReader = new WKTReader();
            for (int x = 0; x < gridCellsCount.X; x++)
            {
                for (int y = 0; y < gridCellsCount.Y; y++)
                {
                    var gridInfoFile = JsonUtility.FromJson<HabitatFieldsTypeInfoJson>( File.ReadAllText(pathGenerator.GridInfoFile(x, y)));

                    string oneCellFilePath = pathGenerator.GridWrtFile(x, y);
                    var tree = new MyQuadtree<HabitatFieldInTree>();
                    var geoCollection = wktReader.Read(File.ReadAllText(oneCellFilePath)) as GeometryCollection;

                    int i = 0;
                    foreach (var type in gridInfoFile.TypesList)
                    {
                        tree.Add(new HabitatFieldInTree()
                        {
                            Field = new HabitatField()
                            {
                                Geometry = geoCollection.GetGeometryN(i),
                                Type = type
                            }
                        });
                        i++;
                    }
                    treesArray[x, y] = tree;
                }
            }

            return new HabitatMap(infoJson.MapStartPosition, infoJson.MapGridSize, infoJson.GridCellsCount, treesArray);
        }

        [Serializable]
        public class HabitatMapInfoJson
        {
            public Vector2 MapStartPosition;
            public Vector2 MapGridSize;
            public IntVector2 GridCellsCount;

            public static HabitatMapInfoJson Create(HabitatMap map)
            {
                return new HabitatMapInfoJson()
                {
                    GridCellsCount = map.GridCellsCount,
                    MapGridSize = map.MapGridSize,
                    MapStartPosition = map.MapStartPosition
                };
            }
        }

        [Serializable]
        public class HabitatFieldsTypeInfoJson
        {
            public List<HabitatType> TypesList;
        }

        private class HabitatMapFileManagerPathsGenerator
        {
            private string _root;

            public HabitatMapFileManagerPathsGenerator(string root)
            {
                _root = root;
            }

            public string MainInfoFilePath()
            {
                return _root + "main.json";
            }

            public string GridWrtFile(int x, int y)
            {
                return _root + $"{x}-{y}.wrt";
            }

            public string GridInfoFile(int x, int y)
            {
                return _root + $"{x}-{y}.json";
            }
        }
    }
}