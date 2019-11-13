using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Utils
{
    public class ConstantSizeClaimableContainer<T>  
    {
        private T[] _internalArray;
        private Queue<uint> _freeIndexes; //TODO much more optimal solution possible
        private Queue<uint> _takenIndexes;

        private ConstantSizeClaimableContainer(T[] internalArray, Queue<uint> freeIndexes, Queue<uint> takenIndexes)
        {
            _internalArray = internalArray;
            _freeIndexes = freeIndexes;
            _takenIndexes = takenIndexes;
        }

        public bool HasFreeSpace()
        {
            return _freeIndexes.Any();
        }

        public bool IsEmpty()
        {
            return !_takenIndexes.Any();
        }

        public uint AddElement(T elem)
        {
            var freeIndex = _freeIndexes.Dequeue();
            _internalArray[freeIndex] = elem;
            _takenIndexes.Enqueue(freeIndex);
            return freeIndex;
        }

        public List<ElementWithIndex<T>> RetriveAllElements()
        {
            return _takenIndexes.Select(i => new ElementWithIndex<T>() {Index = i, Element = _internalArray[i]}).ToList();
        }

        public static ConstantSizeClaimableContainer<T> CreateEmpty(int size)
        {
            return new ConstantSizeClaimableContainer<T>(
                new T[size],
                new Queue<uint>(Enumerable.Range(0, size).Select(c => (uint) c).ToList()),
                new Queue<uint>()
            );
        } 

        public static ConstantSizeClaimableContainer<T> CreateFull(int size)
        {
            return new ConstantSizeClaimableContainer<T>(
                new T[size],
                new Queue<uint>(),
                new Queue<uint>(Enumerable.Range(0, size).Select(c => (uint) c).ToList())
            );
        } 
    }

    public class ElementWithIndex<T>
    {
        public uint Index;
        public T Element;
    }
}
