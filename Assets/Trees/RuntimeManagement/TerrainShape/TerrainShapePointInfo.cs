using UnityEngine;

namespace Assets.Trees.RuntimeManagement.TerrainShape
{
    public class TerrainShapePointInfo
    {
        public Vector3 Normal;
        public float Height;

        public TerrainShapePointInfo(Vector3 normal, float height)
        {
            Normal = normal;
            Height = height;
        }
    }
}