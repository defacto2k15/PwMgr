using System.Collections.Generic;
using System.Linq;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class UnityThreadGpuInstancingOrder
    {
        private List<GpuInstanceAddingOrder> _addingOrders = new List<GpuInstanceAddingOrder>();
        private List<GpuInstanceModifyingOrder> _modifyingOrders = new List<GpuInstanceModifyingOrder>();
        private List<GpuInstanceId> _removingOrders = new List<GpuInstanceId>();
        private bool _anythingThere = false;

        public void InsertAddOrder(GpuInstanceAddingOrder order)
        {
            _addingOrders.Add(order);
            _anythingThere = true;
        }

        public List<GpuInstanceAddingOrder> AddingOrders => _addingOrders;

        public void InsertModifyingOrder(GpuInstanceModifyingOrder gpuInstanceModifyingOrder)
        {
            _modifyingOrders.Add(gpuInstanceModifyingOrder);
            _anythingThere = true;
        }

        public List<GpuInstanceModifyingOrder> ModifyingOrders => _modifyingOrders;

        public void InsertRemovingOrder(GpuInstanceId order)
        {
            _removingOrders.Add(order);
            _anythingThere = true;
        }

        public List<GpuInstanceId> RemovingOrders => _removingOrders;

        public void Clear()
        {
            _addingOrders.Clear();
            _removingOrders.Clear();
            _modifyingOrders.Clear();
            _anythingThere = false;
        }

        public bool AnythingThere => _anythingThere;

    }
}