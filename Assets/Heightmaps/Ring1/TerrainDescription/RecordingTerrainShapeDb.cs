using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class RecordingTerrainShapeDb : ITerrainShapeDb
    {
        private List<TerrainDescriptionQuery> _allQueries = new List<TerrainDescriptionQuery>();
        private ITerrainShapeDb _internalDb;

        public RecordingTerrainShapeDb(ITerrainShapeDb internalDb)
        {
            _internalDb = internalDb;
        }

        public Task<TerrainDescriptionOutput> Query(TerrainDescriptionQuery query)
        {
            Debug.Log("k55");
            _allQueries.Add(query);
            return _internalDb.Query(query);
        }

        public Task DisposeTerrainDetailElement(TerrainDetailElementToken token)
        {
            return _internalDb.DisposeTerrainDetailElement(token);
        }

        public void SaveQueriesToFile(string path)
        {
            Debug.Log("count is " + _allQueries.Count);
            File.WriteAllText(path, JsonUtility.ToJson(TerrainDescriptionQueryListJson.CreateFrom(_allQueries)));
        }

        public static List<TerrainDescriptionQuery> LoadQueriesFromFile(string path)
        {
            return
                TerrainDescriptionQueryListJson.LoadFrom(
                    JsonUtility.FromJson<TerrainDescriptionQueryListJson>(File.ReadAllText(path)));
        }

        [Serializable]
        public class TerrainDescriptionQueryListJson
        {
            public List<SerializableTerrainDescriptionQuery> AllQueries;

            public static TerrainDescriptionQueryListJson CreateFrom(List<TerrainDescriptionQuery> list)
            {
                return new TerrainDescriptionQueryListJson()
                {
                    AllQueries = list.Select(c => SerializableTerrainDescriptionQuery.CreateFrom(c)).ToList()
                };
            }

            public static List<TerrainDescriptionQuery> LoadFrom(TerrainDescriptionQueryListJson list)
            {
                return list.AllQueries.Select(c => SerializableTerrainDescriptionQuery.LoadFrom(c)).ToList();
            }
        }

        [Serializable]
        public class SerializableTerrainDescriptionQuery
        {
            public SerializableMyRectangle QueryArea;
            public List<TerrainDescriptionQueryElementDetail> RequestedElementDetails;

            public static SerializableTerrainDescriptionQuery CreateFrom(TerrainDescriptionQuery query)
            {
                return new SerializableTerrainDescriptionQuery()
                {
                    QueryArea = SerializableMyRectangle.CreateFrom(query.QueryArea),
                    RequestedElementDetails = query.RequestedElementDetails
                };
            }

            public static TerrainDescriptionQuery LoadFrom(SerializableTerrainDescriptionQuery query)
            {
                return new TerrainDescriptionQuery()
                {
                    QueryArea = SerializableMyRectangle.LoadFrom(query.QueryArea),
                    RequestedElementDetails = query.RequestedElementDetails
                };
            }
        }

        [Serializable]
        public class SerializableMyRectangle
        {
            public float X;
            public float Y;
            public float Width;
            public float Height;

            public static SerializableMyRectangle CreateFrom(MyRectangle coords)
            {
                return new SerializableMyRectangle()
                {
                    Height = coords.Height,
                    Width = coords.Width,
                    X = coords.X,
                    Y = coords.Y
                };
            }

            public static MyRectangle LoadFrom(SerializableMyRectangle coords)
            {
                return new MyRectangle(coords.X, coords.Y, coords.Width, coords.Height);
            }
        }

        [Serializable]
        public class SerializableTerrainCardinalResolution
        {
        }
    }
}