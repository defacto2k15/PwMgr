using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Grass.Placing
{
    class UniformRectangleGrassPlacer : IGrassPlacer
    {
        private readonly Vector2 _downLeft;
        private readonly Vector2 _upRight;

        public UniformRectangleGrassPlacer(Vector2 downLeft, Vector2 upRight)
        {
            this._downLeft = downLeft;
            this._upRight = upRight;
        }

        public void Set(GrassEntitiesSet set)
        {
            Vector2 newPlace = RandomGrassPlacingGenerator.GetPositionBetween(_downLeft, _upRight);
            set.TranslateBy(new Vector3(newPlace.x, 0, newPlace.y));
        }
    }

    internal class RandomGrassPlacingGenerator //todo to random
    {
        public static Vector2 GetPositionBetween(Vector2 downLeft, Vector2 upRight)
        {
            var diffrence = downLeft - upRight;
            return downLeft + new Vector2(UnityEngine.Random.value * diffrence.x,
                       UnityEngine.Random.value * diffrence.y);
        }
    }
}