using System.Collections.Generic;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.TerrainShape;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class VegetationSubjectEntitiesDetailEnhancer //todo remove
    {
        private TerrainShapeInformationProvider _terrainShapeInformationProvider;

        public VegetationSubjectEntitiesDetailEnhancer(TerrainShapeInformationProvider terrainShapeInformationProvider)
        {
            _terrainShapeInformationProvider = terrainShapeInformationProvider;
        }

        public List<VegetationSubjectDetailedEntity> AddDetails(List<VegetationSubjectEntity> gainedEntities)
        {
            Preconditions.Fail("SHOULD NOT BE USED");
            var outList = new List<VegetationSubjectDetailedEntity>();
            foreach (var oldEntity in gainedEntities)
            {
                var newPositionInfo = _terrainShapeInformationProvider.RetrivePointInfo(oldEntity.Position2D);

                var newPosition = new Vector3(oldEntity.Position2D.x, newPositionInfo.Height, oldEntity.Position2D.y);
                var baseForwardRotation = new Vector3(1, 0, 1).normalized;
                var newRotation = Quaternion.LookRotation(baseForwardRotation, newPositionInfo.Normal);
                //var newRotation = Vector3.zero;

                var newEntityTriplet = new MyTransformTriplet(newPosition, newRotation, Vector3.one);
                outList.Add(new VegetationSubjectDetailedEntity(oldEntity.Id, newEntityTriplet,
                    newPositionInfo.Normal));
            }
            return outList;
        }
    }
}