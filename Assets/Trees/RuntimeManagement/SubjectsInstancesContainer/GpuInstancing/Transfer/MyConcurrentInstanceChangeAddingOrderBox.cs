using System.Linq;
using System.Threading;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class MyConcurrentInstanceChangeAddingOrderBox
    {
        private GpuInstancingBlocksContainerChangeOrder _value = new GpuInstancingBlocksContainerChangeOrder();
        private object _lock = new object();
        private AutoResetEvent _orderPlacedEvent = new AutoResetEvent(false);

        public GpuInstancingBlocksContainerChangeOrder RetriveOrder()
        {
            lock (_lock)
            {
                //while (!_value.AnythingThere)
                //{
                //    Monitor.Wait(_lock);
                //}
                _orderPlacedEvent.Reset();
                if (_value.AnythingThere)
                {
                    var toReturn = _value;
                    _value = new GpuInstancingBlocksContainerChangeOrder();
                    return toReturn;
                }
                else
                {
                    return GpuInstancingBlocksContainerChangeOrder.Empty;
                }
            }
        }

        public void InsertOrder(UnityThreadGpuInstancingOrder order)
        {
            lock (_lock)
            {
                _value.InsertOrder(order);
                //foreach (var addingOrder in order.AddingOrders)
                //{
                //    _value.InsertAddOrder(addingOrder);
                //}
                //foreach (var removeOrder in order.RemovingOrders)
                //{
                //    _value.InsertRemovingOrder(removeOrder);
                //}

                _orderPlacedEvent.Set();
            }
        }

        public AutoResetEvent OrderPlacedEvent
        {
            get { return _orderPlacedEvent; }
        }
    }
}