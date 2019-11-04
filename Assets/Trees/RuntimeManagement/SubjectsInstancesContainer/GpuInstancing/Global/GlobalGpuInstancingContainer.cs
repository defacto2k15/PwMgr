using System.Collections.Generic;
using System.Linq;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Threading;
using UnityEngine;
using BucketId = System.Int32;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global
{
    public class GlobalGpuInstancingContainer
    {
        private Dictionary<BucketId, GpuInstancingVegetationSubjectContainer> _buckets =
            new Dictionary<int, GpuInstancingVegetationSubjectContainer>();

        private List<GpuInstancingVegetationSubjectContainer> _cachedBucketsList =
            new List<GpuInstancingVegetationSubjectContainer>();

        private bool _cachedListDirty = false;

        private CyclicJobExecutingThread _executor = new CyclicJobExecutingThread();
        private BucketId _lastBucketId = 0;

        public GlobalGpuInstancingContainer(CyclicJobExecutingThread executor = null)
        {
            if (executor != null)
            {
                _executor = executor;
            }
        }

        public BucketId CreateBucket(GpuInstancingVegetationSubjectContainer newContainer)
        {
            _buckets[_lastBucketId] = newContainer;
            _executor.AddJob(newContainer.RetriveCyclicJob());
            _cachedListDirty = true;
            return _lastBucketId++;
        }

        private void UpdateCachedBucketsList()
        {
            _cachedBucketsList = _buckets.Values.ToList();
        }

        public GpuBucketedInstanceId AddInstance(BucketId bucketid, Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            var instanceId = _buckets[bucketid].AddInstance(matrix, uniformsPack);
            return new GpuBucketedInstanceId(instanceId, bucketid);
        }

        public void ModifyInstance(GpuBucketedInstanceId id, Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            _buckets[id.BucketId].ModifyInstance(id.InstanceId, matrix, uniformsPack);
            _cachedListDirty = true;
        }

        public void RemoveInstance(GpuBucketedInstanceId id)
        {
            _buckets[id.BucketId].RemoveInstance(id.InstanceId);
            _cachedListDirty = true;
        }

        public void DrawFrame()
        {
            foreach (var bucket in _cachedBucketsList)
            {
                bucket.DrawFrame();
            }
        }

        public void FinishUpdateBatch()
        {
            if (_cachedListDirty)
            {
                _cachedListDirty = false;
                UpdateCachedBucketsList();
            }

            foreach (var bucket in _cachedBucketsList)
            {
                bucket.FinishUpdateBatch();
            }
            _executor.NonMultithreadUpdate();
        }

        public void StartThread()
        {
            _executor.Start();
        }
    }

    public class GpuBucketedInstanceId
    {
        private GpuInstanceId _instanceId;
        private BucketId _bucketId;

        public GpuBucketedInstanceId(GpuInstanceId instanceId, int bucketId)
        {
            _instanceId = instanceId;
            _bucketId = bucketId;
        }

        public GpuInstanceId InstanceId
        {
            get { return _instanceId; }
        }

        public int BucketId
        {
            get { return _bucketId; }
        }
    }
}