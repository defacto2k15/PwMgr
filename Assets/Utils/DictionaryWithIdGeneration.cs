using System.Collections.Generic;

namespace Assets.Utils
{
    public class DictionaryWithIdGeneration<T>
    {
        private Dictionary<int, T> _dict = new Dictionary<int, T>();
        private int _lastId = 0;

        public int AddNew(T elem)
        {
            _dict[_lastId++] = elem;
            return _lastId - 1;
        }

        public T Get(int key)
        {
            return _dict[key];
        }


        public void Remove(int key)
        {
            Preconditions.Assert(_dict.ContainsKey(key), "There is no key " + key + " in dict");
            _dict.Remove(key);
        }
    }
}