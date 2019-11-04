using System;
using System.Linq;
using Assets.NPR.Curvature;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    public class NPRTrimeshPrincipalCurvatureBufferGenerator
    {
        public float []Generate(Mesh inputMesh,  int radius = 5, bool useKring = true)
        {
            var unaliasedGenerator = new UnaliasedMeshGenerator();
            var unaliasedMesh = unaliasedGenerator.GenerateUnaliasedMesh(inputMesh);

            var verticesFlatArray = unaliasedMesh.Vertices.SelectMany(c => VectorUtils.ToArray((Vector3) c)).ToArray();
            var unaliasedVerticesCount = unaliasedMesh.Vertices.Length;
            var unaliasedTrianglesCount = unaliasedMesh.Triangles.Length / 3;

            var floatsPerVertex = (3 + 1 + 3 + 1 + 4);
            var unaliasedOutArray = new float[unaliasedVerticesCount * floatsPerVertex ];

            var sw = new MyStopWatch();
            sw.StartSegment("trimesh2_compute_principal_curvature - computing");
            PrincipalCurvatureDll.EnableLogging();
            int callStatus = PrincipalCurvatureDll.trimesh2_compute_principal_curvature(verticesFlatArray, unaliasedVerticesCount, unaliasedMesh.Triangles,
                unaliasedTrianglesCount, unaliasedOutArray);
            sw.StartSegment("trimesh2_compute_principal_curvature - aliasing");
            Preconditions.Assert(callStatus == 0, "Calling trimesh2_compute_principal_curvature failed, as returned status "+callStatus);
            //RemapCurvatureData(outDirection1, outDirection2, outValues1, outValues2);

            var inputVerticesCount = inputMesh.vertices.Length;
            var outArray = new float[inputVerticesCount*floatsPerVertex];
            for (int i = 0; i < unaliasedVerticesCount; i++)
            {
                foreach (var inputVerticleIndex in unaliasedMesh.VerticlesToOriginalVerticles[unaliasedMesh.Vertices[i]])
                {
                    for (int j = 0; j < floatsPerVertex; j++)
                    {
                        outArray[inputVerticleIndex * floatsPerVertex + j] = unaliasedOutArray[i * floatsPerVertex + j];
                    }
                }
            }
            Debug.Log("A22: "+sw.CollectResults());
            return outArray;
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

        private void RemapCurvatureData(float[] outDirection1, float[] outDirection2, float[] outValues1, float[] outValues2)
        {
            MakeDirectionsPositive(outDirection1);
            MakeDirectionsPositive(outDirection2);

            var globalMax = Mathf.Max(
                outValues1.Max(),
                outValues2.Max());

            var globalMin = Mathf.Min(
                outValues1.Min(),
                outValues2.Min());

            var globalExtreme = Mathf.Max(Mathf.Abs(globalMax), Mathf.Abs(globalMin));

            for (int i = 0; i < outValues1.Length; i++)
            {
                outValues1[i] = tanhRemap(globalExtreme, outValues1[i]);
                outValues2[i] = tanhRemap(globalExtreme, outValues2[i]);
            }
        }

        private void MakeDirectionsPositive(float[] directions)
        {
            for (int i = 0; i < directions.Length / 3; i++)
            {
                var sign = Math.Sign(directions[i * 3]);
                for (int j = 0; j < 3; j++)
                {
                    directions[i * 3 + j] *= sign;
                }
            }
        }

        private static double ATanh(double x)
        {
            return (Math.Log(1 + x) - Math.Log(1 - x))/2;
        }

        private float tanhRemap(float extreme, float x)
        {
            int b = 32;
            var k = ATanh((Math.Pow(2, b) - 2) / (Math.Pow(2, b) - 1));
            return (float)Math.Tanh((x * k) / extreme);
        }
    }
}