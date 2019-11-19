using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils.MT
{
    public class TcsSemaphore
    {
        private TaskCompletionSource<object> _tcs;

        public TcsSemaphore()
        {
            _tcs = new TaskCompletionSource<object>();
        }

        public Task Await()
        {
            return _tcs.Task;
        }

        public void Set()
        {
            _tcs.SetResult(null);
        }

        public bool SemaphoreIsSet()
        {
            return _tcs.Task.IsCompleted;
        }
    }
}