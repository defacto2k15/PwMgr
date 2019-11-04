using System;
using System.Collections.Generic;
using System.Linq;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;

namespace Assets.Trees.RuntimeManagement
{
    public class VegetationSubjectsVisibleEntitiesContainer
    {
        private readonly Dictionary<VegetationDetailLevel, Quadtree<VegetationSubjectEntity>> _entitiesTreeDict =
            new Dictionary<VegetationDetailLevel, Quadtree<VegetationSubjectEntity>>();

        public VegetationSubjectsVisibleEntitiesContainer()
        {
            foreach (var enumValue in Enum.GetValues(typeof(VegetationDetailLevel)).Cast<VegetationDetailLevel>())
            {
                _entitiesTreeDict.Add(enumValue, new Quadtree<VegetationSubjectEntity>());
            }
        }

        public List<VegetationSubjectEntity> GetAndDeleteEntitiesFrom(IGeometry area, VegetationDetailLevel level)
        {
            var areaEnvelope = area.EnvelopeInternal as Envelope;

            var entitiesInEnvelope = _entitiesTreeDict[level].Query(areaEnvelope)
                .Where(entity => area.Contains(MyNetTopologySuiteUtils.ToPoint(entity.Position2D)))
                .ToList();
            foreach (var entity in entitiesInEnvelope)
            {
                try
                {
                    var removalOk = _entitiesTreeDict[level]
                        .Remove(MyNetTopologySuiteUtils.ToPointEnvelope(entity.Position2D, 0.1f), entity);
                    //Preconditions.Assert(removalOk, "Couldnt remove one vegetationSubjectEntity");
                }
                catch (Exception e)
                {
                    Preconditions.Fail("e44: "+e);
                }

            }
            return entitiesInEnvelope;
        }

        public void AddEntitiesFrom(List<VegetationSubjectEntity> entities, VegetationDetailLevel level)
        {
            foreach (var entity in entities)
            {
                _entitiesTreeDict[level].Insert(MyNetTopologySuiteUtils.ToPointEnvelope(entity.Position2D, 0.1f), entity);
            }
        }
    }
}