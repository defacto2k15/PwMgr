using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class UnityCoordsCalculator
    {
        private Vector2 _globalRing1Size;
        private Vector2 _ring1StartPosition = Vector2.zero;

        public UnityCoordsCalculator(Vector2 globalRingSize)
        {
            _globalRing1Size = globalRingSize;
        }

        public MyRectangle CalculateGlobalObjectPosition(MyRectangle ring1NodePosition)
        {
            return new MyRectangle(
                _globalRing1Size.x * ring1NodePosition.X + _ring1StartPosition.x,
                _globalRing1Size.y * ring1NodePosition.Y + _ring1StartPosition.x,
                _globalRing1Size.x * ring1NodePosition.Width,
                _globalRing1Size.y * ring1NodePosition.Height
            );
        }

        public Vector4 CalculateTextureUvLodOffset(MyRectangle ring1Position)
        {
            return new Vector4(ring1Position.X, ring1Position.Y, 0, 1 / ring1Position.Width);
        }

        public Point2D CalculateGameObjectSize(MyRectangle lodLevel)
        {
            return new Point2D(32, 32);
        }

        public MyRectangle CalculateUvPosition(MyRectangle ring1Position)
        {
            return ring1Position;
        }
    }
}