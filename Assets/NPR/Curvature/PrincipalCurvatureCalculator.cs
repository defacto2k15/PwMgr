using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Vector = MathNet.Numerics.LinearAlgebra.Single.Vector;

namespace Assets.NPR
{
    public class PrincipalCurvatureCalculator
    {
        public Vector3[] CalculateCurvature(Mesh mesh)
        {
            var originalVertices = mesh.vertices;
            var originalTriangles = mesh.triangles;

            var vertices = originalVertices
                .Distinct()
                .ToArray();

            var originalIndexToIndexDict = originalVertices
                .Select(
                    c => vertices
                        .Select((k, i) => new {k, i})
                        .Where(u => c == u.k)
                        .Select(u => u.i).First())
                .ToArray();

            var triangles = originalTriangles
                .Select((c, i) => new {v = c, triangleIndex = i / 3})
                .GroupBy(c => c.triangleIndex)
                .OrderBy(c => c.Key)
                .Select(
                    c => c.Select(k => originalIndexToIndexDict[k.v]).ToList())
                .ToArray();

            var edgeMatricesDict = new Dictionary<VertexPair, Matrix<float>>();

            var maxDiagonal = (float)
                Math.Max( Math.Max(
                        Math.Sqrt(Math.Pow(mesh.bounds.size.x, 2) + Math.Pow(mesh.bounds.size.y, 2)),
                        Math.Sqrt(Math.Pow(mesh.bounds.size.y, 2) + Math.Pow(mesh.bounds.size.z, 2))),
                        Math.Sqrt(Math.Pow(mesh.bounds.size.x, 2) + Math.Pow(mesh.bounds.size.z, 2))
                );


            var outVec = new Vector3[vertices.Length];
            var radius = maxDiagonal * 0.001f;
            for (int i = 0; i < outVec.Length; i++)
            {

                var edgeMatices = new List<Matrix<float>>();
                var usedVertices = new HashSet<int>();

                var queue = new Dictionary<int, float>(); // will work like priority queue
                queue.Add(i, radius);

                while (queue.Any(c => !usedVertices.Contains(c.Key)))
                {
                    var vertexWithDistance = queue.Where(c => !usedVertices.Contains(c.Key)).OrderByDescending(c => c.Value).First();
                    var v = vertexWithDistance.Key;
                    var distanceLeft = vertexWithDistance.Value;

                    usedVertices.Add(v);

                    var vPos = vertices[v];

                    var adjacentToV = triangles
                        .Where(c => c.Contains(v))
                        .SelectMany(c => c)
                        .Where(c => c != v)
                        .Distinct()
                        .ToList();

                    foreach (var v2 in adjacentToV)
                    {
                        var v2Pos = vertices[v2];
                        var d = Vector3.Distance(vPos, v2Pos);

                        if (distanceLeft > 0) //open others
                        {
                            if (!usedVertices.Contains(v2))
                            {
                                var v2DistanceLeft = distanceLeft - d;
                                if (!queue.ContainsKey(v2))
                                {
                                    queue.Add(v2, v2DistanceLeft);
                                }
                                else
                                {
                                    if (queue[v2] < v2DistanceLeft)
                                    {
                                        queue[v2] = v2DistanceLeft;
                                    }
                                }
                            }
                        }

                        if (usedVertices.Contains(v2))
                        {
                            var v2DistanceLeft = queue[v2];
                            if (v2DistanceLeft > 0)
                            {
                                var intersectionDistance = Math.Min(d, v2DistanceLeft);
                                var eVec = (v2Pos - vPos).normalized;

                                var trianglesOfEdge = triangles
                                    .Where(c => c.Contains(v) && c.Contains(v2))
                                    .Select(c => c)
                                    .ToList();

                                if (trianglesOfEdge.Count == 2)
                                {
                                    var t1 = trianglesOfEdge[0];
                                    var t2 = trianglesOfEdge[1];

                                    var tn1 = Vector3.Cross
                                    (
                                        (vertices[t1[1]] - vertices[t1[0]]).normalized,
                                        (vertices[t1[2]] - vertices[t1[0]]).normalized
                                    ).normalized;

                                    var tn2 = Vector3.Cross
                                    (
                                        (vertices[t2[1]] - vertices[t2[0]]).normalized,
                                        (vertices[t2[2]] - vertices[t2[0]]).normalized
                                    ).normalized;

                                    var ang = Mathf.Acos(Vector3.Dot(tn1, tn2));
                                    var beta = Vector3.Dot(tn1, tn2);
                                    beta = ang;

                                    var eVec2 = Vector<float>.Build.DenseOfArray(new float[] {eVec.x, eVec.y, eVec.z}).ToColumnMatrix();

                                    var mat = eVec2.Multiply(eVec2.Transpose());

                                    var t = beta *  intersectionDistance * mat;
                                    edgeMatices.Add(t);

                                }
                                else
                                {
                                    Debug.Log("E32: Triangle count was " + trianglesOfEdge.Count);
                                }
                            }
                        }
                    }

                }
                var sphereArea = (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(radius, 3);
                if (edgeMatices.Any())
                {
                    var theta = (1 / sphereArea) * edgeMatices.Aggregate((sum, next) => sum + next);

                    try
                    {
                        var evd = theta.Evd();
                        var eValues = evd.EigenValues;
                        var eVectors = evd.EigenVectors;
                        var orderedEValueIndexes = eValues
                            .ToList()
                            .Select((c, idx) => new {complex = c, index = idx})
                            .OrderBy(c => c.complex.Magnitude)
                            .Select(c => c.index)
                            .ToList();

                        var normal = VectorUtils.ToVector3(eVectors.Column(orderedEValueIndexes[0]).AsArray()).normalized;
                        var principalCurvature1 = VectorUtils.ToVector3(eVectors.Column(orderedEValueIndexes[1]).AsArray()).normalized;
                        var principalCurvature2 = VectorUtils.ToVector3(eVectors.Column(orderedEValueIndexes[2]).AsArray()).normalized;
                        //outVec[i] = new Vector3((float) eValues[0].Real, (float) eValues[1].Real, (float) eValues[2].Real);
                        outVec[i] = normal;
                    }
                    catch (Exception e)
                    {
                        Debug.Log("LOL!!");
                    }
                }
                else
                {
                    outVec[i] = Vector3.up;
                }
            }

            var toRet = originalIndexToIndexDict
                .Select((p,i) => new {origIndex = i, outV = outVec[p]})
                .OrderBy(c => c.origIndex)
                .Select(c => c.outV)
                .ToArray();
            //for (int i = 0; i < 54; i++)
            //{
            //    toRet[i] = Vector3.one;
            //}

            return toRet;
        }

        public class VertexPair
        {
            private int Vertex1;
            private int Vertex2;

            public VertexPair(int vertexA, int vertexB)
            {
                Vertex1 = vertexA;
                Vertex2 = vertexB;
            }

            protected bool Equals(VertexPair other)
            {
                return Vertex1 == other.Vertex1 && Vertex2 == other.Vertex2;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((VertexPair) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Vertex1 * 397) ^ Vertex2;
                }
            }
        }
    }
}
