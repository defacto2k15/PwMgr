using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Utils
{
    public class LateAssignFactory<T>
    {
        private Func<T> _generatingFunc;
        private T _setElement;

        public LateAssignFactory(Func<T> generatingFunc = null)
        {
            _generatingFunc = generatingFunc;
        }

        public T Retrive()
        {
            if (_generatingFunc != null)
            {
                T element = _generatingFunc();
                Preconditions.Assert(element != null, "Retrived element is null!");
                return element;
            }
            else
            {
                Preconditions.Assert(_setElement != null, "Element was not set");
                return _setElement;
            }
        }

        public void Assign(T element)
        {
            _setElement = element;
        }
    }
}
