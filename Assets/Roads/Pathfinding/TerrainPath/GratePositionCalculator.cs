using Assets.Utils;
using UnityEngine;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class GratePositionCalculator
    {
        private Vector2 _globalStartPosition;
        private float _grateCellLength;

        public GratePositionCalculator(Vector2 globalStartPosition, float grateCellLength)
        {
            _globalStartPosition = globalStartPosition;
            _grateCellLength = grateCellLength;
        }

        public Vector2 ToGlobalPosition(IntVector2 gratePosition)
        {
            return _globalStartPosition + new Vector2(
                       _grateCellLength * gratePosition.X,
                       _grateCellLength * gratePosition.Y
                   );
        }

        public float CalculateManhattanDistance(IntVector2 a, IntVector2 b)
        {
            var delta = IntVector2.Abs(a - b);
            var diagonalSteps = 0; // Math.Min(delta.X, delta.Y);
            var straightSteps = (delta.X + delta.Y) - diagonalSteps;

            return straightSteps * _grateCellLength + diagonalSteps * (straightSteps * Mathf.Sqrt(2));
        }
    }
}