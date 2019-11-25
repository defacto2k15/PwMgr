using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Scheduling;
using Assets.Utils.MT;
using UnityEngine.Profiling;

namespace Assets.Utils.UTUpdating
{

    public abstract class BaseUTProxy<T> : BaseUTService
    {
        protected MyProfilableConcurrentQueue<OrderWithCallerName<T>> _orders = new MyProfilableConcurrentQueue<OrderWithCallerName<T>>();

        private bool _automaticExecution;
        private readonly bool _executeAllOrdersInUpdate;

        protected string _derivedName;

        protected BaseUTProxy(bool automaticExecution = true, bool executeAllOrdersInUpdate = false)
        {
            _automaticExecution = automaticExecution;
            _executeAllOrdersInUpdate = executeAllOrdersInUpdate;
            _derivedName = this.GetType().Name;
        }

        public override UTServiceProfileInfo GetServiceProfileInfo()
        {
            return new UTServiceProfileInfo()
            {
                Name = _derivedName,
                WorkQueueSize = _orders.SometimesWrongCount()
            };
        }

        private MyNamedProfiler _updateMethodProfiler;
        public override float Update()
        {
            var sw = new Stopwatch();
            sw.Start();

            if (_updateMethodProfiler == null)
            {
                _updateMethodProfiler = new MyNamedProfiler(_derivedName + " Update");
            }

            _updateMethodProfiler.BeginSample();
            if (_automaticExecution)
            {
                if (TaskUtils.GetGlobalMultithreading())
                {
                    bool dequeued = true;
                    do
                    {
                        OrderWithCallerName<T> order = null;
                        dequeued = _orders.TryDequeue(out order);
                        if (dequeued)
                        {
                            ExecuteOrderInternal(order);
                        }
                    } while (dequeued && _executeAllOrdersInUpdate);
                }
            }
            InternalUpdate();
            _updateMethodProfiler.EndSample();

            return sw.ElapsedMilliseconds;
        }

        public override bool HasWorkToDo()
        {
            return _orders.Any() || InternalHasWorkToDo();
        }

        public virtual bool InternalHasWorkToDo()
        {
            return false;
        }

        protected virtual void InternalUpdate()
        {
        }

        protected void BaseUtAddOrder(T order, [CallerMemberName] string memberName = "")
        {
            if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
            {
                ExecuteOrderInternal(new OrderWithCallerName<T>()
                {
                    Order = order,
                    CallerName = memberName
                });
            }
            else
            {
                _orders.Enqueue(
                    new OrderWithCallerName<T>()
                    {
                        Order = order,
                        CallerName = memberName
                    });
            }
        }

        private void ExecuteOrderInternal(OrderWithCallerName<T> order)
        {
            MyProfiler.BeginSample(_derivedName + " Execute: " + order.CallerName);
            ExecuteOrderInternal2(order.Order);
            MyProfiler.EndSample();
        }

        protected abstract void ExecuteOrderInternal2(T order);
    }

    public abstract class BaseUTConsumer<T> : BaseUTProxy<T>
    {
        protected BaseUTConsumer(bool automaticExecution = true, bool executeAllOrdersInUpdate = false) : base(automaticExecution, executeAllOrdersInUpdate)
        {
        }

        protected override void ExecuteOrderInternal2(T order)
        {
            ExecuteOrder(order);
        }

        protected abstract void ExecuteOrder(T order);
    }

    public abstract class BaseUTTransformProxy<TReturn, TOrder> : BaseUTProxy<UTProxyTransformOrder<TReturn, TOrder>>
    {
        protected BaseUTTransformProxy(bool automaticExecution = true) : base(automaticExecution)
        {
        }

        public override UTServiceProfileInfo GetServiceProfileInfo()
        {
            return new UTServiceProfileInfo()
            {
                Name = _derivedName,
                WorkQueueSize = _orders.SometimesWrongCount()
            };
        }

        protected Task<TReturn> BaseUtAddOrder(TOrder order, [CallerMemberName] string memberName = "")
        {
            var tcs = new TaskCompletionSource<TReturn>();
            BaseUtAddOrder(new UTProxyTransformOrder<TReturn, TOrder>()
            {
                Order = order,
                Tcs = tcs
            });
            return tcs.Task;
        }

        protected abstract TReturn ExecuteOrder(TOrder order);

        protected override void ExecuteOrderInternal2(UTProxyTransformOrder<TReturn, TOrder> transformOrder)
        {
            var result = ExecuteOrder(transformOrder.Order);
            transformOrder.Tcs.SetResult(result);
        }

        protected UTProxyTransformOrder<TReturn, TOrder> TryGetNextFromQueue()
        {
            OrderWithCallerName<UTProxyTransformOrder<TReturn, TOrder>> order;
            bool succeded = _orders.TryDequeue(out order);
            if (!succeded)
            {
                return null;
            }
            else
            {
                return order.Order;
            }
        }
    }

    public class OrderWithCallerName<TOrder>
    {
        public TOrder Order;
        public string CallerName;
    }

    public abstract class LegacyBaseUTProxy : IUpdatable
    {
        public float Update()
        {
            var sw = new Stopwatch();
            sw.Start();
            if (TaskUtils.GetGlobalMultithreading())
            {
                InternalUpdate();
            }
            return sw.ElapsedMilliseconds;
        }

        public abstract void InternalUpdate();
    }

    public class UTProxyTransformOrder<TReturn, TOrder>
    {
        public TaskCompletionSource<TReturn> Tcs;
        public TOrder Order;
    }


    public abstract class BaseUTService : IUpdatable
    {
        public abstract float Update();
        public abstract bool HasWorkToDo();

        public abstract UTServiceProfileInfo GetServiceProfileInfo();
    }
}