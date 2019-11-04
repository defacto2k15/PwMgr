using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Curvature
{
    public class MeshCurvatureDetailGenerator
    {
        public MeshCurvatureDetailSE Generate(UnaliasedMesh mesh, int radius = 5, bool useKring = true)
        {
            //var originalVertices = mesh.vertices;
            //var originalTriangles = mesh.triangles;

            //var verticlesToOriginalVerticles = new Dictionary<Vector3, List<int>>();
            //for (int i = 0; i < originalVertices.Length; i++)
            //{
            //    var vert = originalVertices[i];
            //    if (!verticlesToOriginalVerticles.ContainsKey(vert))
            //    {
            //        verticlesToOriginalVerticles[vert] = new List<int>();
            //    }
            //    verticlesToOriginalVerticles[vert].Add(i);
            //}

            //var vertices = verticlesToOriginalVerticles.Keys.ToArray();

            //var originalIndexToIndexDict = new int[originalVertices.Length];
            //for (int i = 0; i < vertices.Length; i++)
            //{
            //    var v = vertices[i];
            //    foreach (var originalIndex in verticlesToOriginalVerticles[v])
            //    {
            //        originalIndexToIndexDict[originalIndex] = i;
            //    }
            //}

            //var triangles = originalTriangles
            //    .Select(c => originalIndexToIndexDict[c])
            //    .ToArray();

            var verticesFlatArray = mesh.Vertices.SelectMany(c => c.ToArray()).ToArray();
            var verticesCount = mesh.Vertices.Length;
            var trianglesCount = mesh.Triangles.Length / 3;

            var outDirection1 = new float[3 * verticesCount];
            var outDirection2 = new float[3 * verticesCount];
            var outValues1 = new float[verticesCount];
            var outValues2 = new float[verticesCount];

            PrincipalCurvatureDll.EnableLogging();
            int callStatus = PrincipalCurvatureDll.compute_principal_curvature(verticesFlatArray,verticesCount, mesh.Triangles,trianglesCount,outDirection1,outDirection2,
                outValues1,outValues2, radius, useKring);
            Preconditions.Assert(callStatus == 0, "Calling compute_principal_curvature failed, as returned status "+callStatus);

            var od1 = FlatArrayToVectorArray(outDirection1);
            var od2 = FlatArrayToVectorArray(outDirection2);

            return MeshCurvatureDetailSE.CreateDetail(
                    mesh.OriginalIndexToIndex.Select(c => od1[c]).ToArray(),
                    mesh.OriginalIndexToIndex.Select(c => od2[c]).ToArray(),
                    mesh.OriginalIndexToIndex.Select(c => outValues1[c]).ToArray(),
                    mesh.OriginalIndexToIndex.Select(c => outValues2[c]).ToArray()
                );
        }

        private static Vector3[] FlatArrayToVectorArray(float[] flatArray)
        {
            Preconditions.Assert(flatArray.Length %3 == 0, "Input array length must be divible by 3, but is "+flatArray.Length);
            return flatArray
                .Select((c, i) => new {v = c, index = i})
                .GroupBy(c => c.index / 3)
                .Select(c => c.ToArray())
                .Select(c => new Vector3(c[0].v, c[1].v, c[2].v))
                .ToArray();
        }
    }
}
