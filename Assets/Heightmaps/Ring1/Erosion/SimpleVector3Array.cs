using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class SimpleVector3Array : MySimpleArray<Vector3>
    {
        public SimpleVector3Array(Vector3[,] array) : base(array)
        {
        }

        public SimpleVector3Array(int x, int y) : base(x, y)
        {
        }
    }
}