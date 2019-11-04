using UnityEngine;

namespace Assets.Trees.RuntimeManagement.TerrainShape
{
    public class TerrainShapeInformationProvider
    {
        public TerrainShapePointInfo RetrivePointInfo(Vector2 position)
        {
            var epsilon = new Vector2(0.01f, -0.01f);
            var firstDir =
                new Vector3(epsilon.x, 10 * shapeFunc(position + new Vector2(epsilon.x, epsilon.x)), epsilon.x) -
                new Vector3(epsilon.y, 10 * shapeFunc(position + new Vector2(epsilon.y, epsilon.y)), epsilon.y);

            var secondDir =
                new Vector3(epsilon.y, 10 * shapeFunc(position + new Vector2(epsilon.y, epsilon.x)), epsilon.x) -
                new Vector3(epsilon.x, 10 * shapeFunc(position + new Vector2(epsilon.x, epsilon.y)), epsilon.y);

            var normal = Vector3.Cross(firstDir.normalized, secondDir.normalized);
            var pointHeight = shapeFunc(position);

            return new TerrainShapePointInfo(normal, pointHeight);
        }

        private float shapeFunc(Vector2 position)
        {
            return (Mathf.Sin(position.x / 10f) + Mathf.Cos(position.y / 10f));
        }
    }
}