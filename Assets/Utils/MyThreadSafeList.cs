using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public class MyThreadSafeList<T>
    {
        private readonly object _syncLock = new object();
        private readonly List<T> _list = new List<T>();

        public List<T> TakeAll()
        {
            List<T> outList;
            lock (_syncLock)
            {
                outList = new List<T>(_list);
                _list.Clear();
            }
            return outList;
        }

        public void Add(T newObj)
        {
            lock (_syncLock)
            {
                _list.Add(newObj);
            }
        }
    }
}