using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class GpuInstancingBlocksContainerChangeOrder
    {

        private List<UnityThreadGpuInstancingOrder> _orderList = new List<UnityThreadGpuInstancingOrder>();
        private bool _anythingThere = false;

        public IEnumerable<GpuInstanceAddingOrder> AddingOrders
        {
            get { return _orderList.SelectMany(c => c.AddingOrders); }
        }

        public IEnumerable<GpuInstanceModifyingOrder> ModifyingOrders
        {
            get { return _orderList.SelectMany(c => c.ModifyingOrders); }
        }

        public IEnumerable<GpuInstanceId> RemovingOrders
        {
            get { return _orderList.SelectMany(c => c.RemovingOrders); }
        }

        public bool AnythingThere => _anythingThere;

        private static readonly GpuInstancingBlocksContainerChangeOrder EmptySingleton =
            new GpuInstancingBlocksContainerChangeOrder();

        public static GpuInstancingBlocksContainerChangeOrder Empty => EmptySingleton;

        public void InsertOrder(UnityThreadGpuInstancingOrder order)
        {
            _anythingThere = true;
            _orderList.Add(order);    
        }
    }
}