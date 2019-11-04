using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Trees.Db
{
    public class DebugColoringVegetationSubjectsDatabase : IVegetationSubjectsDatabase
    {
        private readonly Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>> _subjects
            = new Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>>();

        public DebugColoringVegetationSubjectsDatabase()
        {
            foreach (var level in Enum.GetValues(typeof(VegetationLevelRank)).Cast<VegetationLevelRank>())
            {
                _subjects[level] = new Quadtree<VegetationSubject>();
            }
        }

        public void AddSubject(VegetationSubject subject, VegetationLevelRank levelRank)
        {
            var newObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            newObject.transform.position = new Vector3(subject.XzPosition.x, 0, subject.XzPosition.y); //todo delete

            var vegetationType = subject.CreateCharacteristics.CurrentVegetationType;

            newObject.GetComponent<Renderer>().material.color = SpeciesTypeToColor(vegetationType);

            _subjects[levelRank].Insert(CreateEnvelope(subject), subject);
        }

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

        public List<VegetationSubject> GetSubjectsAt(GenerationArea area)
        {
            return _subjects.SelectMany(c => c.Value.Query(CreateEnvelope(area))).ToList();
        }

        public Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>> Subjects => _subjects;

        public Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>> RetriveAllSubjects()
        {
            throw new NotImplementedException();
        }


        private Color SpeciesTypeToColor(VegetationSpeciesEnum input)
        {
            if (input == VegetationSpeciesEnum.Tree1A)
            {
                return new Color(0, 0.3f, 0);
            }
            if (input == VegetationSpeciesEnum.Tree2A)
            {
                return new Color(0, 0.6f, 0);
            }
            if (input == VegetationSpeciesEnum.Tree3A)
            {
                return new Color(0, 1.0f, 0);
            }

            if (input == VegetationSpeciesEnum.Tree1B)
            {
                return new Color(0.3f, 0, 0);
            }
            if (input == VegetationSpeciesEnum.Tree2B)
            {
                return new Color(0.6f, 0, 0);
            }
            if (input == VegetationSpeciesEnum.Tree3B)
            {
                return new Color(1.0f, 0, 0);
            }

            if (input == VegetationSpeciesEnum.Tree1C)
            {
                return new Color(0, 0, 0.3f);
            }
            if (input == VegetationSpeciesEnum.Tree2C)
            {
                return new Color(0, 0, 0.6f);
            }
            if (input == VegetationSpeciesEnum.Tree3C)
            {
                return new Color(0, 0, 1.0f);
            }
            return new Color(0, 0, 0);
        }
    }
}