using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1BoundsCalculator
    {
        private readonly UnityCoordsCalculator _globalObjectPositionCalculator;

        public Ring1BoundsCalculator(UnityCoordsCalculator globalObjectPositionCalculator)
        {
            _globalObjectPositionCalculator = globalObjectPositionCalculator;
        }

        public Bounds CalculateBounds(MyRectangle ring1NodePosition)
        {
            MyRectangle inGamePosition =
                _globalObjectPositionCalculator.CalculateGlobalObjectPosition(ring1NodePosition);
            return new Bounds(
                new Vector3(inGamePosition.X + inGamePosition.Width / 2, 0,
                    inGamePosition.Y + inGamePosition.Width / 2),
                new Vector3(inGamePosition.Width, 10, inGamePosition.Height)
            );
        }
    }
}