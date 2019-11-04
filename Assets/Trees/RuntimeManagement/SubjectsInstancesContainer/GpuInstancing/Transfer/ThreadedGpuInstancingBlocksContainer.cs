using System;
using System.Threading;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Threading;
using Assets.Utils.MT;
using NetTopologySuite.Shape.Fractal;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class ThreadedGpuInstancingBlocksContainer
    {
        private GpuInstancingBlocksContainer _blocksContainer;

        private MyConcurrentInstanceChangeAddingOrderBox _addingOrderBox =
            new MyConcurrentInstanceChangeAddingOrderBox();

        private GpuInstanceRenderingBoxData _renderingData = new GpuInstanceRenderingBoxData();

        public ThreadedGpuInstancingBlocksContainer(GpuInstancingUniformsArrayTemplate uniformsArrayTemplate)
        {
            _blocksContainer = new GpuInstancingBlocksContainer(uniformsArrayTemplate);
        }

        public CyclicJobWithWait RetriveJob()
        {
            Action job = (() =>
            {
                var order = _addingOrderBox.RetriveOrder();
                _blocksContainer.Update(order, _renderingData);
            });
            var eventToWaitFor = _addingOrderBox.OrderPlacedEvent;
            return new CyclicJobWithWait(job, eventToWaitFor);
        }


        private UnityThreadGpuInstancingOrder _currentOrder = new UnityThreadGpuInstancingOrder();
        //private object _currentOrderLock = new object();
        private MyPatientLock _currentOrderLock = new MyPatientLock();

        public GpuInstanceId AddInstance(Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            var instanceId = GpuInstanceId.CreateEmpty();
            using (var access = _currentOrderLock.EnterNotImportant())
            {
                _currentOrder.InsertAddOrder(new GpuInstanceAddingOrder(instanceId, matrix, uniformsPack));
            }
            return instanceId;
        }

        public void ModifyInstance(GpuInstanceId instanceId, Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            using (var access = _currentOrderLock.EnterNotImportant())
            {
                _currentOrder.InsertModifyingOrder(new GpuInstanceModifyingOrder(instanceId, matrix, uniformsPack));
            }
        }

        public void RemoveInstance(GpuInstanceId removedIdx)
        {
            using (var access = _currentOrderLock.EnterNotImportant())
            {
                _currentOrder.InsertRemovingOrder(removedIdx);
            }
        }

        public void FinishUpdateBatch()
        {
            if (_currentOrder.AnythingThere)
            {
                UnityThreadGpuInstancingOrder oldOrder;

                using (var access = _currentOrderLock.EnterNotImportant())
                {
                    if (!access.HasLock)
                    {
                        return;
                    }
                    oldOrder = _currentOrder;
                    _currentOrder = new UnityThreadGpuInstancingOrder();
                }
                _addingOrderBox.InsertOrder(oldOrder);
            }
        }

        public GpuInstanceRenderingBoxData RenderingData
        {
            get { return _renderingData; }
        }
    }

}