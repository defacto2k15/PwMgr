using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.NPR.Curvature;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    class NPRPrincipalCurvatureBufferGenerator
    {
        public float []Generate(Mesh inputMesh,  int radius = 5, bool useKring = true)
        {
            var unaliasedGenerator = new UnaliasedMeshGenerator();
            var unaliasedMesh = unaliasedGenerator.GenerateUnaliasedMesh(inputMesh);

            var verticesFlatArray = unaliasedMesh.Vertices.SelectMany(c => c.ToArray()).ToArray();
            var unaliasedVerticesCount = unaliasedMesh.Vertices.Length;
            var unaliasedTrianglesCount = unaliasedMesh.Triangles.Length / 3;

            var outDirection1 = new float[3 * unaliasedVerticesCount];
            var outDirection2 = new float[3 * unaliasedVerticesCount];
            var outValues1 = new float[unaliasedVerticesCount];
            var outValues2 = new float[unaliasedVerticesCount];

            var sw = new MyStopWatch();
            sw.StartSegment("pc");
            PrincipalCurvatureDll.EnableLogging();
            int callStatus = PrincipalCurvatureDll.compute_principal_curvature(verticesFlatArray,unaliasedVerticesCount, unaliasedMesh.Triangles,unaliasedTrianglesCount,outDirection1,outDirection2,
                outValues1,outValues2, radius, useKring);
            sw.StartSegment("pc2");
            Preconditions.Assert(callStatus == 0, "Calling compute_principal_curvature failed, as returned status "+callStatus);
            //RemapCurvatureData(outDirection1, outDirection2, outValues1, outValues2);

            var inputVerticesCount = inputMesh.vertices.Length;
            var outArray = new float[inputVerticesCount * 8];
            for (int i = 0; i < unaliasedVerticesCount; i++)
            {
                foreach (var inputVerticleIndex in unaliasedMesh.VerticlesToOriginalVerticles[unaliasedMesh.Vertices[i]])
                {
                    var outArrayIdx = inputVerticleIndex * 8;
                    var inArrayIdx = i * 3;
                    outArray[outArrayIdx + 0] = outDirection1[inArrayIdx + 0];
                    outArray[outArrayIdx + 1] = outDirection1[inArrayIdx + 1];
                    outArray[outArrayIdx + 2] = outDirection1[inArrayIdx + 2];

                    outArray[outArrayIdx + 3] = outValues1[i];

                    outArray[outArrayIdx + 4] = outDirection2[inArrayIdx + 0];
                    outArray[outArrayIdx + 5] = outDirection2[inArrayIdx + 1];
                    outArray[outArrayIdx + 6] = outDirection2[inArrayIdx + 2];

                    outArray[outArrayIdx + 7] = outValues2[i];
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
