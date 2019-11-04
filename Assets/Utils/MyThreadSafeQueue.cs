using System.Collections.Generic;

namespace Assets.Utils
{
    public class MyThreadSafeQueue<T> where T : class
    {
        private readonly object _syncLock = new object();
        private readonly Queue<T> _queue = new Queue<T>();

        public void Enqueue(T element)
        {
            lock (_syncLock)
            {
                _queue.Enqueue(element);
            }
        }

        public T Dequeue()
        {
            lock (_syncLock)
            {
                if (_queue.Count == 0)
                {
                    return null;
                }
                return _queue.Dequeue();
            }
        }

        public bool IsEmpty()
        {
            lock (_syncLock)
            {
                return _queue.Count == 0;
            }
        }
    }
}