using Assets.Utils;

namespace Assets.Roads.Engraving
{
    public class PathProximityArray
    {
        private PathProximityInfo[,] _array;

        public PathProximityArray(int height, int width)
        {
            _array = new PathProximityInfo[height, width];
        }

        public void SetProximity(int x, int y, PathProximityInfo newInfo)
        {
            if (_array[x, y] == null || _array[x, y].Distance > newInfo.Distance)
            {
                _array[x, y] = newInfo;
            }
        }

        public IntVector2 Size => new IntVector2(_array.GetLength(0), _array.GetLength(1));

        public PathProximityInfo GetProximityInfo(int x, int y)
        {
            return _array[x, y];
        }
    }
}