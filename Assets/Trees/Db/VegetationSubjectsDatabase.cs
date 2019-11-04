using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Trees.Placement;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;

namespace Assets.Trees.Db
{
    public class VegetationSubjectsDatabase : IVegetationSubjectsDatabase
    {
        private readonly Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>> _subjects
            = new Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>>();

        public VegetationSubjectsDatabase()
        {
            foreach (var level in Enum.GetValues(typeof(VegetationLevelRank)).Cast<VegetationLevelRank>())
            {
                _subjects[level] = new Quadtree<VegetationSubject>();
            }
        }

        public void AddSubject(VegetationSubject subject, VegetationLevelRank levelRank)
        {
            _subjects[levelRank].Insert(CreateEnvelope(subject), subject);
        }

        public List<VegetationSubject> GetSubjectsAt(GenerationArea area)
        {
            return _subjects.SelectMany(c => c.Value.Query(CreateEnvelope(area))).ToList();
        }

        public Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>> Subjects => _subjects;

        private Envelope CreateEnvelope(VegetationSubject subject)
        {
            var radius = subject.ExclusionRadius;
            var xzPosition = subject.XzPosition;
            return new Envelope(xzPosition.x - radius, xzPosition.x + radius, xzPosition.y - radius,
                xzPosition.y + radius);
        }

        private Envelope CreateEnvelope(GenerationArea subject)
        {
            return new Envelope(subject.X, subject.EndX, subject.Y, subject.EndY);
        }
    }
}