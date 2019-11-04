using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Assets.Utils
{
    public class MyThreadSafeBlockingQueue<T> where T : class
    {
        private object _lock = new object();
        private readonly Queue<T> _queue = new Queue<T>();

        public void Enqueue(T element)
        {
            lock (_lock)
            {
                bool shouldPulse = !_queue.Any();
                _queue.Enqueue(element);
                if (shouldPulse)
                {
                    Monitor.Pulse(_lock);
                }
            }
        }

        public T Dequeue()
        {
            lock (_lock)
            {
                while (!_queue.Any())
                {
                    Monitor.Wait(_lock);
                }
                return _queue.Dequeue();
            }
        }
    }
}