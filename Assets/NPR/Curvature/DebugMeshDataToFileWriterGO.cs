using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Curvature
{
    public class DebugMeshDataToFileWriterGO : MonoBehaviour
    {
        public String MeshName;
        public String RootDirectory = @"C:/tmp/mesh/";

        public void Start()
        {
            if (RootDirectory != null)
            {
                var mesh = GetComponent<MeshFilter>().sharedMesh;
                var curvatureData = GetComponent<PrincipalCurvatureInjectOC>().CurvatureDetail;

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

                var verticesFlatArray = vertices.SelectMany(c => c.ToArray()).ToArray();

                using (var stream = new FileStream(RootDirectory+MeshName+".vertArray.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (float item in verticesFlatArray)
                    {
                        writer.Write(item);
                    }
                }

                var triangles = originalTriangles
                    .Select(c => originalIndexToIndexDict[c])
                    .ToArray();

                using (var stream = new FileStream(RootDirectory+MeshName+".triangleArray.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (int item in triangles)
                    {
                        writer.Write(item);
                    }
                }

                using (var stream = new FileStream(RootDirectory+MeshName+".infoArray.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(vertices.Length);
                    writer.Write(triangles.Length/3);
                }

                var pd1 = vertices
                    .Select(c => verticlesToOriginalVerticles[c][0])
                    .Select(c => curvatureData.PrincipalDirection1[c])
                    .SelectMany(c => c.ToArray())
                    .ToArray();

                var pd2 = vertices
                    .Select(c => verticlesToOriginalVerticles[c][0])
                    .Select(c => curvatureData.PrincipalDirection2[c])
                    .SelectMany(c => c.ToArray())
                    .ToArray();

                var pv1 = vertices.Select(c => verticlesToOriginalVerticles[c][0]).Select(c => curvatureData.PrincipalValue1[c]).ToArray();
                var pv2 = vertices.Select(c => verticlesToOriginalVerticles[c][0]).Select(c => curvatureData.PrincipalValue2[c]).ToArray();

                using (var stream = new FileStream(RootDirectory+MeshName+".pd1.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (float item in pd1)
                    {
                        writer.Write(item);
                    }
                }

                using (var stream = new FileStream(RootDirectory+MeshName+".pd2.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (float item in pd2)
                    {
                        writer.Write(item);
                    }
                }

                using (var stream = new FileStream(RootDirectory+MeshName+".pv1.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (float item in pv1)
                    {
                        writer.Write(item);
                    }
                }

                using (var stream = new FileStream(RootDirectory+MeshName+".pv2.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (float item in pv2)
                    {
                        writer.Write(item);
                    }
                }
            }
        }
    }
}
