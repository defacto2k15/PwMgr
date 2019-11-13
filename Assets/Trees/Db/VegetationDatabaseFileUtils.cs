using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.FinalExecution;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement;
using Assets.Trees.RuntimeManagement;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Trees.Db
{
    public class VegetationDatabaseFileUtils
    {
        public static void WriteToFile(string path, IVegetationSubjectsDatabase db)
        {
            var elements = (db.Subjects
                .SelectMany(c =>
                    c.Value.QueryAll().Select(k => new VegetationSubjectJson()
                    {
                        Pos2D = k.XzPosition,
                        Size = k.CreateCharacteristics.RangedSize,
                        Radius = k.ExclusionRadius,
                        SpeciesEnum = k.CreateCharacteristics.CurrentVegetationType,
                        Rank = c.Key
                    }))).ToList();
            var asList = new VegetationSubjectJsonList()
            {
                Elements = elements
            };
            var jsonAsString = JsonUtility.ToJson(asList, true);
            File.WriteAllText(path, jsonAsString);
        }

        [Serializable]
        public class VegetationSubjectJsonList
        {
            public List<VegetationSubjectJson> Elements;
        }

        [Serializable]
        public class VegetationSubjectJson
        {
            public Vector2 Pos2D;
            public float Size;
            public float Radius;
            public VegetationSpeciesEnum SpeciesEnum;
            public VegetationLevelRank Rank;
        }

        public static List<VegetationSubjectJson> LoadRawFromFile(string path)
        {
            var fileContents = File.ReadAllText(path);
            var reserialized = JsonUtility.FromJson<VegetationSubjectJsonList>(fileContents);
            return reserialized.Elements;
        }

        public static Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>> LoadFromFile(string path)
        {
            var fileContents = File.ReadAllText(path);
            var reserialized = JsonUtility.FromJson<VegetationSubjectJsonList>(fileContents);

            Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>> outDict =
                new Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>>();
            foreach (var rank in Enum.GetValues(typeof(VegetationLevelRank)).Cast<VegetationLevelRank>())
            {
                outDict.Add(rank, new Quadtree<VegetationSubjectEntity>());
            }

            foreach (var json in reserialized.Elements)
            {
                var rank = json.Rank;
                var detail = new DesignBodyLevel0Detail()
                {
                    Pos2D = json.Pos2D,
                    Radius = json.Radius,
                    Size = json.Size,
                    SpeciesEnum = json.SpeciesEnum
                };
                outDict[rank].Insert(CreateEnvelope(detail), new VegetationSubjectEntity(detail));
            }

            return outDict;
        }

        public static Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>> LoadFromFiles(string path)
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Loading trees from json");
            Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>> outDict =
                new Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>>();
            foreach (var rank in Enum.GetValues(typeof(VegetationLevelRank)).Cast<VegetationLevelRank>())
            {
                outDict.Add(rank, new Quadtree<VegetationSubjectEntity>());
            }

            var allFiles = Directory.GetFiles(path);
            foreach (var singleFilePath in allFiles)
            {
                AddFromFile(singleFilePath, outDict);
            }
            Debug.Log("L97: " + msw.CollectResults());

            return outDict;
        }

        private static void AddFromFile(string path,
            Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>> outDict)
        {
            var fileContents = File.ReadAllText(path);
            var reserialized = JsonUtility.FromJson<VegetationSubjectJsonList>(fileContents);
            foreach (var json in reserialized.Elements)
            {
                var rank = json.Rank;
                var detail = new DesignBodyLevel0Detail()
                {
                    Pos2D = json.Pos2D,
                    Radius = json.Radius,
                    Size = json.Size,
                    SpeciesEnum = json.SpeciesEnum
                };
                outDict[rank].Insert(CreateEnvelope(detail), new VegetationSubjectEntity(detail));
            }
        }

        private static void AddFromFile(string path,
            Dictionary<VegetationLevelRank, List<VegetationSubjectEntity>> outDict)
        {
            var fileContents = File.ReadAllText(path);
            var reserialized = JsonUtility.FromJson<VegetationSubjectJsonList>(fileContents);
            foreach (var json in reserialized.Elements)
            {
                var rank = json.Rank;
                var detail = new DesignBodyLevel0Detail()
                {
                    Pos2D = json.Pos2D,
                    Radius = json.Radius,
                    Size = json.Size,
                    SpeciesEnum = json.SpeciesEnum
                };
                outDict[rank].Add( new VegetationSubjectEntity(detail));
            }
        }

        private static Envelope CreateEnvelope(DesignBodyLevel0Detail detail)
        {
            const float epsylon = 0.001f;
            var xzPos = detail.Pos2D;
            return new Envelope(xzPos.x - epsylon, xzPos.x + epsylon, xzPos.y - epsylon, xzPos.y + epsylon);
        }

        public static void WriteToFileNonOverwrite(string path, VegetationSubjectsDatabase newDb)
        {
            FileAttributes attr = File.GetAttributes(path);

            Preconditions.Assert((attr & FileAttributes.Directory) == FileAttributes.Directory,
                "Given path must be directory: " + path);
            string fileName = "db_" + DateTime.Today.Ticks + ".json";

            WriteToFile(Path.Combine(path, fileName), newDb);
        }

        public static void WriteRawToFile(List<VegetationSubjectJson> buffer, string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(new VegetationSubjectJsonList()
            {
                Elements = buffer
            }));
        }

        public  static Dictionary<VegetationLevelRank, List<VegetationSubjectEntity>> LoadListFromFiles(string path)
        {
            var outDict = new Dictionary<VegetationLevelRank, List<VegetationSubjectEntity>>();
            foreach (var rank in Enum.GetValues(typeof(VegetationLevelRank)).Cast<VegetationLevelRank>())
            {
                outDict.Add(rank, new List<VegetationSubjectEntity>());
            }

            var allFiles = Directory.GetFiles(path);
            foreach (var singleFilePath in allFiles)
            {
                AddFromFile(singleFilePath, outDict);
            }

            return outDict;
        }
    }
}