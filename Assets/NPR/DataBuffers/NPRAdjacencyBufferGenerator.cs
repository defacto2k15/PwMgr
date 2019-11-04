using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.NPR
{
    public class NPRAdjacencyBufferGenerator
    {
        public Vector3[] GenerateTriangleAdjacencyBuffer( Mesh mesh)
        {
            // dla kazdego trojkata - okresl adjacency
            var adjacencyInfos = new List<AdjacencyInformation>();

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var normals = mesh.normals;

            var positionToIndexDict = vertices
                .Select((v, i) => new {v, i})
                .GroupBy(c => c.v)
                .ToDictionary(
                    c => c.Key,
                    c => c.Select(k => k.i).ToList());

            var indexToTriangleDict = triangles
                .Select((v, i) => new {triangleIndex = i / 3, v})
                .GroupBy(c => c.v)
                .ToDictionary(c => c.Key, c => c.Select(k => k.triangleIndex).ToList());
            
            for (int triangleIndex = 0; triangleIndex < triangles.Length/3; triangleIndex++)
            {
                var index0 = triangles[triangleIndex * 3 + 0];
                var vertex0 = vertices[index0];

                var index1 = triangles[triangleIndex * 3 + 1];
                var vertex1 = vertices[index1];

                var index2 = triangles[triangleIndex * 3 + 2];
                var vertex2 = vertices[index2];

                var otherTrianglesOfVertex0 = FindOtherTrianglesOfVertex(positionToIndexDict, indexToTriangleDict, vertex0, triangleIndex);
                var otherTrianglesOfVertex1 = FindOtherTrianglesOfVertex(positionToIndexDict, indexToTriangleDict, vertex1, triangleIndex);
                var otherTrianglesOfVertex2 = FindOtherTrianglesOfVertex(positionToIndexDict, indexToTriangleDict, vertex2, triangleIndex);
                
                AdjacencyInformation info = new AdjacencyInformation();

                FillAdjacencyInformation(vertices, triangles, normals, otherTrianglesOfVertex0, otherTrianglesOfVertex1, triangleIndex, index0, index1, info, 0);
                FillAdjacencyInformation(vertices, triangles, normals, otherTrianglesOfVertex1, otherTrianglesOfVertex2, triangleIndex, index1, index2, info, 1);
                FillAdjacencyInformation(vertices, triangles, normals, otherTrianglesOfVertex2, otherTrianglesOfVertex0, triangleIndex, index2, index0, info, 2);

                info.SetNormal(0, positionToIndexDict[vertex0].Select(i => normals[i]).Aggregate((sum, next) => sum+next).normalized);
                info.SetNormal(1, positionToIndexDict[vertex1].Select(i => normals[i]).Aggregate((sum, next) => sum+next).normalized);
                info.SetNormal(2, positionToIndexDict[vertex2].Select(i => normals[i]).Aggregate((sum, next) => sum+next).normalized);

                adjacencyInfos.Add(info);
            }

            var buffer = new Vector3[adjacencyInfos.Count * 6];
            for (int i = 0; i < adjacencyInfos.Count; i++)
            {
                adjacencyInfos[i].WriteToBuffer(buffer, i);
            }

            return buffer;
        }

        private static void FillAdjacencyInformation(Vector3[] vertices, int[] triangles, Vector3[] normals,
            List<int> otherTrianglesOfVertexA, List<int> otherTrianglesOfVertexB, int triangleIndex,
            int indexA, int indexB, AdjacencyInformation info, int indexInInfo)
        {
            var commonTrianglesOfEdge0 = otherTrianglesOfVertexA.Intersect(otherTrianglesOfVertexB).ToList();
            if (commonTrianglesOfEdge0.Count > 1)
            {
                Debug.Log("W139 More than one common triangles of edge of triangle ");
            }

            if (commonTrianglesOfEdge0.Count == 0)
            {
                Debug.Log("W138 No common triangles at edge of triangle");
                info.SetPosition(indexInInfo, Vector3.negativeInfinity);
            }
            else
            {
                var adjacentTriangle = commonTrianglesOfEdge0[0];

                var tv = new List<int>()
                    {
                        triangles[adjacentTriangle * 3 + 0],
                        triangles[adjacentTriangle * 3 + 1],
                        triangles[adjacentTriangle * 3 + 2],
                    }.Select(c => new { v = c, pos = vertices[c] })
                    .Where(c => c.pos != vertices[indexA] && c.pos != vertices[indexB])
                    .ToList();
                if (tv.Count != 1)
                {
                    Debug.Log("W135 TV list has not one but " + tv.Count);
                    info.SetPosition(indexInInfo, Vector3.negativeInfinity);
                }
                else
                {
                    var thirdVertex = tv.First().v;
                    info.SetPosition(indexInInfo, vertices[thirdVertex]);
                }
            }
        }

        private static List<int> FindOtherTrianglesOfVertex(Dictionary<Vector3, List<int>> positionToIndexDict, Dictionary<int, List<int>> indexToTriangleDict,
            Vector3 thisVertex, int thisVertexTriangle)
        {
            var vList0 = positionToIndexDict[thisVertex];
            var index = thisVertexTriangle;
            return  vList0
                .SelectMany(c => indexToTriangleDict[c])
                .Distinct()
                .Where(c => c != index)
                .ToList();
        }
    }
}