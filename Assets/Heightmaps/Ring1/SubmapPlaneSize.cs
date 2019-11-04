using Assets.Utils;

namespace Assets.Heightmaps.Ring1
{
    public class SubmapPlaneSize
    {
        private readonly int _width;
        private readonly int _height;
        private readonly bool _constantSize;

        public SubmapPlaneSize(int width, int height, bool constantSize)
        {
            this._width = width;
            this._height = height;
            _constantSize = constantSize;
        }

        public SubmapPlaneSize GetQuarter()
        {
            if (_constantSize)
            {
                return this;
            }
            else
            {
                Preconditions.Assert(_width % 2 == 0, "Width is not power of two");
                Preconditions.Assert(_height % 2 == 0, "Height is not power of two");
                return new SubmapPlaneSize(_width / 2, _height / 2, _constantSize);
            }
        }

        public Point2D GameObjectSize
        {
            get { return new Point2D(_width + 1, _height + 1); }
        }
    }
}