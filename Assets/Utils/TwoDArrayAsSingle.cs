namespace Assets.Utils
{
    public class TwoDArrayAsSingle<T>
    {
        private readonly int _width;
        private readonly int _height;
        private T[] _array;

        public TwoDArrayAsSingle(int width, int height)
        {
            _width = width;
            _height = height;
            _array = new T[_width * _height];
        }

        public void Set(int x, int y, T value)
        {
            _array[y * _width + x] = value;
        }

        public int Width => _width;

        public int Height => _height;

        public T[] Array => _array;
    }
}