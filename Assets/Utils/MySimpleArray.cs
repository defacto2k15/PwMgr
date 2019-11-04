using Assets.Heightmaps.Ring1.Erosion;

namespace Assets.Utils
{
    public class MySimpleArray<T>
    {
        protected T[,] _array;

        public MySimpleArray(T[,] array)
        {
            _array = array;
        }

        public MySimpleArray(int x, int y)
        {
            _array = new T[x, y];
        }

        public T GetValue(int x, int y)
        {
            return _array[x, y];
        }

        public T GetValue(IntVector2 point)
        {
            return GetValue(point.X, point.Y);
        }

        public virtual void SetValue(int x, int y, T value) //todo out virtual
        {
            _array[x, y] = value;
        }

        public void SetValue(IntVector2 point, T value)
        {
            SetValue(point.X, point.Y, value);
        }

        public int Width => _array.GetLength(0);

        public int Height => _array.GetLength(1);

        public HeightArrayBoundaries Boundaries => new HeightArrayBoundaries(Width, Height);
        public T[,] Array => _array;
    }
}