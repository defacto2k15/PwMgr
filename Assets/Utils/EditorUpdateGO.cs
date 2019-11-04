using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Utils.Editor
{
    public class EditorUpdateGO : MonoBehaviour
    {
        private Dictionary<MonoBehaviour, UpdateOrder> _orders = new Dictionary<MonoBehaviour, UpdateOrder>();

        public void AddOrder(MonoBehaviour caller, Action action, float cycleTime)
        {
            _orders[caller] = new UpdateOrder(action, 0, cycleTime);
        }

        public void RemoveOrder(MonoBehaviour caller)
        {
            _orders.Remove(caller);
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
            foreach (var order in _orders.Values)
            {
                if (order.ShouldUpdate(Time.realtimeSinceStartup))
                {
                    order.Update(Time.realtimeSinceStartup);
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
                _cyclicAction = cyclicAction;
                _lastUpdateTime = lastUpdateTime;
                _cycleTime = cycleTime;
            }

            public bool ShouldUpdate(float currentTime)
            {
                return currentTime - _lastUpdateTime > _cycleTime;
            }

            public void Update(float currentTime)
            {
                _lastUpdateTime = currentTime;
                _cyclicAction();
            }
        }
    }
}
