using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.MeshGeneration;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass
{
    class GrassMeshGenerator
    {
        private readonly Dictionary<int, Mesh> meshCache = new Dictionary<int, Mesh>();

        public static Mesh GenerateGrassBladeMesh(int levelsCount)
        {
            levelsCount = 7 - levelsCount;
            Preconditions.Assert(levelsCount >= 1, "levelsCount must be >= 1");
            Func<float, float> leftOffsetGenerator = (percent) =>
            {
                return 1.0f - (float) Math.Pow(-percent + 1.0f, 0.5f);
            };
            Mesh mesh = new Mesh();
            mesh.Clear();

            var sw = 1.0f; //standard width
            var sh = 1.0f; //standard height
            var df = 0.5f; //decrease factor

            int vertexCount = 2 * levelsCount + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int currLev = 0; currLev < levelsCount; currLev++)
            {
                float height = sh * ((float) currLev / levelsCount);
                float offsetFromLeft = leftOffsetGenerator((float) currLev / levelsCount) * (sw / 2);
                var pos1 = new Vector3(offsetFromLeft, height);
                var pos2 = new Vector3(sw - offsetFromLeft, height);
                vertices[currLev * 2] = pos1;
                vertices[currLev * 2 + 1] = pos2;

                uvs[currLev * 2] = new Vector2(pos1.x, pos1.y);
                uvs[currLev * 2 + 1] = new Vector2(pos2.x, pos2.y);
            }
            vertices[vertexCount - 1] = new Vector3(sw / 2, sh);
            uvs[vertexCount - 1] = new Vector2(sw / 2, sh);

            // vertex offset unmaking making
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= new Vector3(sw / 2, 0, 0);
            }


            Vector3[] normales = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                normales[i] = new Vector3(0.4f, 0.4f, 0.4f);
            }


            var trianglesCount = 2 * levelsCount + 1;
            int[] triangles = new int[3 * trianglesCount];
            for (int currLev = 0; currLev < levelsCount - 1; currLev++)
            {
                triangles[currLev * 6] = currLev * 2;
                triangles[currLev * 6 + 1] = currLev * 2 + 1;
                triangles[currLev * 6 + 2] = currLev * 2 + 3;
                triangles[currLev * 6 + 3] = currLev * 2;
                triangles[currLev * 6 + 4] = currLev * 2 + 3;
                triangles[currLev * 6 + 5] = currLev * 2 + 2;
            }
            triangles[3 * trianglesCount - 3] = vertexCount - 3;
            triangles[3 * trianglesCount - 2] = vertexCount - 2;
            triangles[3 * trianglesCount - 1] = vertexCount - 1;


            triangles = MeshGenerationUtils.makeTrianglesDoubleSided(triangles);

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            ;
            return mesh;
        }

        public Mesh GetGrassBladeMesh(int turfCount)
        {
            if (!meshCache.ContainsKey(turfCount))
            {
                meshCache[turfCount] = GenerateGrassBladeMesh(turfCount);
            }
            return meshCache[turfCount];
        }

        private readonly Dictionary<Vector2, Mesh> _billboardMeshCache = new Dictionary<Vector2, Mesh>();

        public Mesh GetGrassBillboardMesh(float minUv, float maxUv)
        {
            var vec = new Vector2(minUv, maxUv);
            if (!_billboardMeshCache.ContainsKey(vec))
            {
                _billboardMeshCache[vec] = GenerateGrassBillboardMesh(minUv, maxUv);
            }
            return _billboardMeshCache[vec];
        }

        private Mesh GenerateGrassBillboardMesh(float minUv, float maxUv)
        {
            Mesh mesh = new Mesh();
            mesh.Clear();

            int resX = 2;
            int resZ = 2;

            float width = 1;
            float height = 1;

            #region Vertices

            //todo remove regions to refactor to methods when resharper is present!
            Vector3[] vertices = new Vector3[resX * resZ];
            for (int z = 0; z < resZ; z++)
            {
                float zPos = ((float) z / (resZ - 1));
                for (int x = 0; x < resX; x++)
                {
                    float xPos = ((float) x / (resX - 1));
                    vertices[x + z * resX] = new Vector3(xPos - (width / 2), zPos, 0);
                }
            }

            Vector3[] normales = new Vector3[vertices.Length];
            for (int n = 0; n < normales.Length; n++)
                normales[n] = Vector3.up;

            #endregion

            #region UVs

            float uvLength = maxUv - minUv;
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int v = 0; v < resZ; v++)
            {
                for (int u = 0; u < resX; u++)
                {
                    uvs[u + v * resX] = new Vector2(minUv + ((float) u / (resX - 1)) * uvLength,
                        (float) v / (resZ - 1));
                }
            }

            #endregion

            #region Triangles

            int nbFaces = (resX - 1) * (resZ - 1);
            int[] triangles = new int[nbFaces * 6];
            int t = 0;
            for (int face = 0; face < nbFaces; face++)
            {
                // Retrieve lower left corner from face ind
                int i = face % (resX - 1) + (face / (resX - 1) * resX);

                triangles[t++] = i + resX;
                triangles[t++] = i + 1;
                triangles[t++] = i;

                triangles[t++] = i + resX;
                triangles[t++] = i + resX + 1;
                triangles[t++] = i + 1;
            }

            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}