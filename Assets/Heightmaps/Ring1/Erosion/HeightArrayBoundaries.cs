using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class HeightArrayBoundaries
    {
        private int _width;
        private int _height;

        public HeightArrayBoundaries(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public bool AreValidIndexes(IntVector2 p)
        {
            return p.X >= 0 && p.X < _width && p.Y >= 0 && p.Y < _height;
        }
    }
}