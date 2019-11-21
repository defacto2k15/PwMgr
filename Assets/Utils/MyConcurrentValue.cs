using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public class MyConcurrentValue<T>
    {
        private T _value = default(T);
        private object _lock = new object();

        public T Value
        {
            get
            {
                lock (_lock)
                {
                    return _value;
                }
            }

            set
            {
                lock (_lock)
                {
                    _value = value;
                }
            }
        }
    }
}