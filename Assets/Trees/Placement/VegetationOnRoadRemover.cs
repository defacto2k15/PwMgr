using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using Assets.Roads;
using Assets.Roads.Files;
using Assets.TerrainMat;
using Assets.Trees.Db;
using Assets.Trees.DesignBodyDetails;
using Assets.Utils;
using Assets.Utils.Quadtree;
using NetTopologySuite.Index.Quadtree;
using OsmSharp;
using UnityEngine;

namespace Assets.Trees.Placement
{
    public class VegetationOnRoadRemover
    {
        private readonly RoadDatabaseProxy _roadDb;
        private readonly VegetationOnRoadRemoverConfiguration _configuration;

        public VegetationOnRoadRemover(RoadDatabaseProxy roadDb, VegetationOnRoadRemoverConfiguration configuration)
        {
            _configuration = configuration;
            _roadDb = roadDb;
        }

        public VegetationSubjectsDatabase RemoveCollidingTrees(VegetationSubjectsDatabase db,
            MyRectangle removalArea)
        {
            var allSubjectsDict = db.Subjects;
            var removalCellSize = _configuration.RemovalCellSize;

            var allRemovedSubjectsSet = new HashSet<VegetationSubject>();

            for (float x = removalArea.X; x < removalArea.MaxX; x += _configuration.RemovalCellSize.x)
            {
                for (float y = removalArea.Y; y < removalArea.MaxY; y += _configuration.RemovalCellSize.y)
                {
                    var removalCell = new MyRectangle(x, y, removalCellSize.x, removalCellSize.y);
                    allRemovedSubjectsSet.AddRange(RemoveInCell(allSubjectsDict, removalCell));
                }
            }

            var newSubjectsDb = new VegetationSubjectsDatabase();
            foreach (var pair in db.Subjects.SelectMany(c => c.Value.QueryAll().Select(k => new
            {
                rank = c.Key,
                subject = k
            })))
            {
                if (!allRemovedSubjectsSet.Contains(pair.subject))
                {
                    newSubjectsDb.AddSubject(pair.subject, pair.rank);
                }
            }

            return newSubjectsDb;
        }

        private HashSet<VegetationSubject> RemoveInCell(
            Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>> allSubjectsDict,
            MyRectangle removalCell)
        {
            //var removalCellEnvelope = MyNetTopologySuiteUtils.ToEnvelope(removalCell);
            var removalCellGeo = MyNetTopologySuiteUtils.ToGeometryEnvelope(removalCell);


            var roadsOfArea = _roadDb.Query(removalCell).Result.Select(c => c.Line)
                .Select(c => c.Intersection(removalCellGeo)).Where(c => !c.IsEmpty).ToList();

            var allRemovedSubjectsSet = new HashSet<VegetationSubject>();

            foreach (var rank in allSubjectsDict.Keys)
            {
                var perRankRemovedSubjects = new HashSet<VegetationSubject>();

                var removalPropabilityRange = _configuration.PerDistanceRemovalPropabilities[rank];
                var maxRange = removalPropabilityRange.Max;

                foreach (var road in roadsOfArea)
                {
                    var enlargedRoadEnvelope = road.EnvelopeInternal.ToUnityCoordPositions2D()
                        .EnlagreByMargins(maxRange);

                    var subjectsInZone = allSubjectsDict[rank].Query(enlargedRoadEnvelope.ToEnvelope())
                        .Where(c => !perRankRemovedSubjects.Contains(c));

                    foreach (var subject in subjectsInZone)
                    {
                        var distance = MyNetTopologySuiteUtils.Distance(road, subject.XzPosition, maxRange);
                        var random = new RandomProvider(subject.XzPosition.GetHashCode()); //todo unified vec2 to ssed

                        var cutoffDistance = removalPropabilityRange.Lerp(random.NextValue);
                        if (distance < cutoffDistance)
                        {
                            perRankRemovedSubjects.Add(subject);
                        }
                    }
                }
                allRemovedSubjectsSet.AddRange(perRankRemovedSubjects);
            }

            return allRemovedSubjectsSet;
        }
    }

    public class VegetationOnRoadRemoverConfiguration
    {
        public Vector2 RemovalCellSize;
        public Dictionary<VegetationLevelRank, MyRange> PerDistanceRemovalPropabilities;
    }
}