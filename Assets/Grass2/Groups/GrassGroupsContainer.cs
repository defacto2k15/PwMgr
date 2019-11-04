using System.Collections.Generic;
using System.Linq;
using Assets.Grass2.Planting;
using Assets.Ring2.Devising;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using UnityEngine;

namespace Assets.Grass2.Groups
{
    public class GrassGroupsContainer
    {
        private GlobalGpuInstancingContainer _gpuInstancingContainer;
        private int _bucketId;

        public GrassGroupsContainer(GlobalGpuInstancingContainer gpuInstancingContainer, int bucketId)
        {
            _gpuInstancingContainer = gpuInstancingContainer;
            _bucketId = bucketId;
        }

        private Dictionary<GrassGroupId, List<GpuBucketedInstanceId>> _groupsIdsDict
            = new Dictionary<GrassGroupId, List<GpuBucketedInstanceId>>();

        public void AddGroup(List<Grass2PositionedEntity> positionedEntities, GrassGroupId id)
        {
            var list = positionedEntities.Select(c => _gpuInstancingContainer.AddInstance(_bucketId,
                c.LocalToWorldMatrix, c.Uniforms)).ToList();

            _groupsIdsDict[id] = list;

            //foreach (var entity in positionedEntities)
            //{
            //    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    var pos = entity.LocalToWorldMatrix.ExtractPosition();
            //    cube.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
            //    cube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            //    cube.GetComponent<MeshRenderer>().material.color = Color.red;
            //}
        }

        public void RemoveGroup(GrassGroupId groupId)
        {
            _groupsIdsDict[groupId].ForEach(c => _gpuInstancingContainer.RemoveInstance(c));
            _groupsIdsDict.Remove(groupId);
        }
    }

    public class Grass2PositionedEntity
    {
        public Matrix4x4 LocalToWorldMatrix;
        public UniformsPack Uniforms;
    }
}