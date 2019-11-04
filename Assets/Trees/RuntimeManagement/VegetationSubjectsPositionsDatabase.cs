using System.Collections.Generic;
using System.Linq;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement
{
    public class VegetationSubjectsPositionsDatabase : IVegetationSubjectsPositionsProvider
    {
        private readonly Quadtree<VegetationSubjectEntity> _subjectsTree;

        public VegetationSubjectsPositionsDatabase(Quadtree<VegetationSubjectEntity> subjectsTree)
        {
            _subjectsTree = subjectsTree;
        }

        public List<VegetationSubjectEntity> GetEntiesFrom(IGeometry area, VegetationDetailLevel level)
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Finding");
            var envelope = area.EnvelopeInternal as Envelope;

            var allFoundEnties =
                _subjectsTree.Query(envelope)
                    .Where(c => area.Contains(MyNetTopologySuiteUtils.ToPoint(c.Position2D)))
                    .Select(c => CastEntityToAppropiateLevel(c, level))
                    .ToList();
            return allFoundEnties;
        }

        private VegetationSubjectEntity CastEntityToAppropiateLevel(VegetationSubjectEntity oldEntity,
            VegetationDetailLevel level)
        {
            var idOffset = VegetationDetailLevelUtils.GetLevelIdOffset(level);
            return new VegetationSubjectEntity(oldEntity, oldEntity.Id + idOffset);
        }
    }
}