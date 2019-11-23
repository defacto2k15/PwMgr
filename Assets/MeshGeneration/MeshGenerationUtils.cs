using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.MeshGeneration
{
    class MeshGenerationUtils
    {
        public static Mesh CloneMesh(Mesh inputMesh)
        {
            var newMesh = new Mesh();
            newMesh.vertices = inputMesh.vertices.ToArray();
            newMesh.triangles = inputMesh.triangles.ToArray();
            newMesh.normals = inputMesh.normals.ToArray();
            newMesh.tangents = inputMesh.tangents.ToArray();
            newMesh.uv = inputMesh.uv.ToArray();
            newMesh.RecalculateBounds();
            return newMesh;
        }


        public static int[] makeTrianglesDoubleSided(int[] oldTriangles)
        {
            Preconditions.Assert(oldTriangles.Length % 3 == 0,
                string.Format("Triangle array has length {0}, which is not divisible by 3 ", oldTriangles.Length));
            var newTriangles = new int[oldTriangles.Length * 2];
            Array.Copy(oldTriangles, newTriangles, oldTriangles.Length);
            for (var i = 0; i < oldTriangles.Length / 3; i++)
            {
                newTriangles[oldTriangles.Length + i * 3] = oldTriangles[i * 3];
                newTriangles[oldTriangles.Length + i * 3 + 1] = oldTriangles[i * 3 + 2];
                newTriangles[oldTriangles.Length + i * 3 + 2] = oldTriangles[i * 3 + 1];
            }

            return newTriangles;
        }


        public static void calculateMeshTangents(Mesh mesh)
        {
            //speed up math by copying the mesh arrays
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            Vector3[] normals = mesh.normals;

            //variable definitions
            int triangleCount = triangles.Length;
            int vertexCount = vertices.Length;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            Vector4[] tangents = new Vector4[vertexCount];

            for (long a = 0; a < triangleCount; a += 3)
            {
                long i1 = triangles[a + 0];
                long i2 = triangles[a + 1];
                long i3 = triangles[a + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = uv[i1];
                Vector2 w2 = uv[i2];
                Vector2 w3 = uv[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1.0f / (s1 * t2 - s2 * t1);

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }


            for (long a = 0; a < vertexCount; ++a)
            {
                Vector3 n = normals[a];
                Vector3 t = tan1[a];

                //Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
                //tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
                Vector3.OrthoNormalize(ref n, ref t);
                tangents[a].x = t.x;
                tangents[a].y = t.y;
                tangents[a].z = t.z;

                tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }

            mesh.tangents = tangents;
        }

        public static void SetYBoundsToInfinity(Mesh mesh)
        {
            var oldBounds = mesh.bounds;
            mesh.bounds = new Bounds(oldBounds.center, new Vector3(oldBounds.size.x, 100000000, oldBounds.size.z));
        }

        public static void SetYBounds(Mesh mesh, float min, float max)
        {
            var newYCenter = min + (max - min) / 2f;
            var oldBounds = mesh.bounds;
            mesh.bounds = new Bounds(new Vector3(oldBounds.center.x, newYCenter, oldBounds.center.z),
                new Vector3(oldBounds.size.x, (max - min), oldBounds.size.z));
        }

        public static void OffsetVertices(Mesh mesh, Vector3 offset)
        {
            mesh.vertices = mesh.vertices.Select(c => c + offset).ToArray();
        }

        public static Mesh CreateMeshAsSum(List<CombineInstance> elements)
        {
            var vertexCount = elements.Select(c => c.mesh.vertexCount).Sum();
            var trianglesCount = elements.Select(c => c.mesh.triangles.Length/3).Sum();

            var verticesArray = new Vector3[vertexCount];
            var trianglesArray = new int[trianglesCount*3];
            var uvs = new Vector2[vertexCount];

            var vertexOffset = 0;
            var triangleOffset = 0;
            foreach (var anElement in elements)
            {
                var mesh = anElement.mesh;

                var transformedVertices = mesh.vertices.Select(c => anElement.transform.MultiplyPoint(c)).ToArray();
                Array.Copy(transformedVertices, 0, verticesArray, vertexOffset, mesh.vertices.Length);
                var transformedTriangles = mesh.triangles.Select(c => c+ vertexOffset).ToArray();
                Array.Copy(transformedTriangles, 0, trianglesArray, triangleOffset, mesh.triangles.Length);
                Array.Copy(mesh.uv, 0, uvs, vertexOffset, mesh.uv.Length);

                vertexOffset += mesh.vertexCount;
                triangleOffset += mesh.triangles.Length ;
            }

            var outMesh = new Mesh()
            {
                indexFormat = IndexFormat.UInt32,
                vertices = verticesArray,
                triangles = trianglesArray,
                uv =  uvs
            };

            outMesh.RecalculateNormals();
            outMesh.RecalculateTangents();
            outMesh.RecalculateBounds();

            return outMesh;
        }

        public static void RecalculateUvAsInPlane(Mesh mesh)
        {
            var xRange = new Vector2(mesh.vertices.Select(c => c.x).Min(), mesh.vertices.Select(c => c.x).Max());
            var zRange = new Vector2(mesh.vertices.Select(c => c.z).Min(), mesh.vertices.Select(c => c.z).Max());

            Debug.Log("XR is "+xRange+" yr is "+zRange);
            mesh.uv = mesh.vertices.Select(c => new Vector2((c.x - xRange[0]) / (xRange[1] - xRange[0]), (c.z - zRange[0]) / (zRange[1] - zRange[0])))
                .ToArray();
        }
    }
}