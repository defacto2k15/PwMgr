using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils
{
    public class UnaliasedMeshGenerator
    {
        public UnaliasedMesh GenerateUnaliasedMesh(Mesh mesh)
        {
            var originalVertices = mesh.vertices;
            var originalTriangles = mesh.triangles;

            var verticlesToOriginalVerticles = new Dictionary<Vector3, List<int>>();
            for (int i = 0; i < originalVertices.Length; i++)
            {
                var vert = originalVertices[i];
                if (!verticlesToOriginalVerticles.ContainsKey(vert))
                {
                    verticlesToOriginalVerticles[vert] = new List<int>();
                }

                verticlesToOriginalVerticles[vert].Add(i);
            }

            var vertices = verticlesToOriginalVerticles.Keys.ToArray();

            var originalIndexToIndexDict = new int[originalVertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                foreach (var originalIndex in verticlesToOriginalVerticles[v])
                {
                    originalIndexToIndexDict[originalIndex] = i;
                }
            }

            var triangles = originalTriangles
                .Select(c => originalIndexToIndexDict[c])
                .ToArray();

            return new UnaliasedMesh()
            {
                OriginalIndexToIndex = originalIndexToIndexDict,
                Triangles = triangles,
                Vertices = vertices,
                VerticlesToOriginalVerticles = verticlesToOriginalVerticles
            };
        }
    }

    public class UnaliasedMesh
    {
        public Vector3[] Vertices;
        public int[] Triangles;
        public Dictionary<Vector3, List<int>> VerticlesToOriginalVerticles;
        public int[] OriginalIndexToIndex;
    }
}
