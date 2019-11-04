using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public class LateAssignBox<T>
    {
        private T _element;

        public void Set(T element)
        {
            Preconditions.Assert(_element == null, "Element inside box was arleady set");
            _element = element;
        }

        public T Get()
        {
            Preconditions.Assert(_element != null, "Element in box was NOT set");
            return _element;
        }
    }
}