using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Utils.Editor
{
    [ExecuteInEditMode]
    public class EditorUpdate2GO : MonoBehaviour
    {
        private Dictionary<string, UpdateOrder> _orders = new Dictionary<string, UpdateOrder>();
        public bool AutomaticUpdate;

        public void ClearAllOrders()
        {
            _orders.Clear();
        }

        public void SetOrder(string callerName, Action action, float cycleTime)
        {
            _orders[callerName] = new UpdateOrder(action, 0, cycleTime);
        }

        public void RemoveOrder(string name)
        {
            if (_orders.ContainsKey(name))
            {
                _orders.Remove(name);
            }
            else
            {
                Debug.Log("W234 There is no such order!");
            }
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.update += OnEditorUpdate;
#endif
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
#endif
        }

        protected virtual void OnEditorUpdate()
        {
            if (AutomaticUpdate)
            {
                foreach (var order in _orders.Values)
                {
                    if (order.ShouldUpdate(Time.realtimeSinceStartup))
                    {
                        order.Update(Time.realtimeSinceStartup);
                    }
                }
            }
        }

        private class UpdateOrder
        {
            private Action _cyclicAction;
            private float _lastUpdateTime;
            private float _cycleTime;

            public UpdateOrder(Action cyclicAction, float lastUpdateTime, float cycleTime)
            {
                Preconditions.Assert(cycleTime >= 1, "Cycle time must be bigger-equal one, but is "+cycleTime);
                _cyclicAction = cyclicAction;
                _lastUpdateTime = lastUpdateTime;
                _cycleTime = cycleTime;
            }

            public bool ShouldUpdate(float currentTime)
            {
                return (currentTime - _lastUpdateTime) > _cycleTime;
            }

            public void Update(float currentTime)
            {
                _lastUpdateTime = currentTime;
                _cyclicAction();
            }
        }
    }
}
