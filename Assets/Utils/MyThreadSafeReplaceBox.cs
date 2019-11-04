using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.Utils
{
    public class MyThreadSafeOneSpaceBox<T> where T : class
    {
        private object _lock = new object();
        private T _data = null;

        public void SetCurrentData(T newData)
        {
            lock (_lock)
            {
                bool shouldPulse = _data == null;
                _data = newData;
                if (shouldPulse)
                {
                    Monitor.Pulse(_lock);
                }
            }
        }

        public T RetriveUpdateData()
        {
            lock (_lock)
            {
                while (_data == null)
                {
                    Monitor.Wait(_lock);
                }
                var toReturn = _data;
                _data = null;
                return toReturn;
            }
        }
    }
}