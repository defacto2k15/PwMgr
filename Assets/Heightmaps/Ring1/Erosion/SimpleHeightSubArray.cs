using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class SimpleHeightSubArray
    {
        private readonly int _yStart;
        private readonly int _subArrayHeight;
        private float[,] _array;

        public SimpleHeightSubArray(int yStart, int subArrayHeight, int width)
        {
            _yStart = yStart;
            _subArrayHeight = subArrayHeight;
            _array = new float[width, subArrayHeight];
        }

        public void SetValue(IntVector2 point, float value)
        {
            Preconditions.Assert(point.Y >= _yStart && point.Y < _yStart + _subArrayHeight, "Y not in area");
            _array[point.X, point.Y - _yStart] = value;
        }

        public void AddValue(IntVector2 point, float value)
        {
            SetValue(point, GetValue(point) + value);
        }

        public float GetValue(IntVector2 point)
        {
            Preconditions.Assert(point.Y >= _yStart && point.Y < _yStart + _subArrayHeight, "Y not in area");
            float value = 0;
            value = _array[point.X, point.Y - _yStart];
            return value;
        }

        public int YStart => _yStart;

        public float[,] Array => _array;

        public bool HasLineFor(int y)
        {
            return y >= _yStart && y < _yStart + _subArrayHeight;
        }
    }
}