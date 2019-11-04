using Assets.Utils;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class ColorPack
    {
        private readonly Color[] _array;

        public ColorPack(Color[] array)
        {
            this._array = array;
        }

        public Color GetColor(int idx)
        {
            return _array[idx];
        }

        protected bool Equals(ColorPack other)
        {
            return Equals(_array, other._array);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ColorPack) obj);
        }

        public override int GetHashCode()
        {
            return (_array != null ? CalculateArrayHashCode(_array) : 0);
        }

        private int CalculateArrayHashCode(Color[] array)
        {
            int hash = array.Length;
            for (int i = 0; i < array.Length; i++)
            {
                hash = unchecked ( hash * 12345 + array[i].GetHashCode());
            }
            return hash;
        }

        public bool[] SingleEquality(ColorPack other)
        {
            bool[] outArray = new[]
            {
                _array[0] == other._array[0],
                _array[1] == other._array[1],
                _array[2] == other._array[2],
                _array[3] == other._array[3]
            };
            return outArray;
        }

        public Color this[int i]
        {
            get { return _array[i]; }
            set { _array[i] = value; }
        }

        public override string ToString()
        {
            return $"{nameof(_array)}: {StringUtils.ToString(_array)}";
        }
    }
}