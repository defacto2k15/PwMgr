using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Assets.Utils.UTUpdating;

namespace Assets.Utils
{
    public class MyProfilableConcurrentQueue<T>
    {
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private int _count = 0;

        public bool TryDequeue(out T element)
        {
            bool res = _queue.TryDequeue(out element);
            if (res)
            {
                Interlocked.Decrement(ref _count);
            }
            return res;
        }

        public int SometimesWrongCount()
        {
            return _count;
        }

        public bool Any()
        {
            return _count != 0;
        }

        public void Enqueue(T element)
        {
            _queue.Enqueue(element);
            Interlocked.Increment(ref _count);
        }
    }
}
