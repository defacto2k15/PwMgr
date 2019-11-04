using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees
{
    public class TreeMeshSymplifyingDebugObject : MonoBehaviour
    {
        public Mesh BaseMesh;

        public void Start()
        {
            List<Vector3> vertices = new List<Vector3>();
            BaseMesh.GetVertices(vertices);

            List<int> indices = new List<int>();
            BaseMesh.GetIndices(indices, 1);

            List<Vector2> uvs = new List<Vector2>();
            BaseMesh.GetUVs(0, uvs);

            var meshTemplate = new MeshTemplate(vertices, indices, uvs);

            var meldVerticesList = meshTemplate.CreateMeldVerticesList();
            var meldVertexToColor = new int[meldVerticesList.Count];
            var lastColorIndex = 1;

            foreach (var aMeldVertex in meldVerticesList.MeldVertices)
            {
                if (meldVertexToColor[aMeldVertex.Id] == 0)
                {
                    var color = lastColorIndex++;
                    meldVertexToColor[aMeldVertex.Id] = color;

                    var toExpand = new List<MeldVertex>()
                    {
                        aMeldVertex
                    };
                    while (toExpand.Any())
                    {
                        var neighbours = toExpand.SelectMany(c => meldVerticesList.NeighboursOf(c)).ToList();
                        //Debug.Log("NC: " + neighbours.Count() + " TO " + toExpand.Count);
                        toExpand.Clear();
                        foreach (var aNeighbour in neighbours)
                        {
                            if (meldVertexToColor[aNeighbour.Id] == 0)
                            {
                                meldVertexToColor[aNeighbour.Id] = color;
                                toExpand.Add(aNeighbour);
                            }
                        }
                    }
                }
            }
            var branchesVertLists = meldVertexToColor.Select((c, i) => new
            {
                c,
                i
            })
                .GroupBy(x => x.c)
                .Where(x => x.Count() > 1)
                .Select(c => (c.Select(k => meldVerticesList.MeldVertices[k.i]).ToList())).ToList();


            var theBranchList = branchesVertLists[0];
            Debug.Log("BBR: "+branchesVertLists.Count+"  cx "+theBranchList.Count);
            Debug.Log("A1: "+theBranchList[0].Vertices.Count);

            //var branch = ExtractBranch(theBranchList, meldVerticesList);
            //Debug.Log("R1: " + branch.Circles.Count);

            //foreach (var vertexIdx in branch.Circles[2].Vertices.SelectMany(c => c.Vertices))
            //{
            //    meshTemplate.Vertices[vertexIdx] = Vector3.left;
            //}
            //meldVerticesList.MeldVertices[0].Vertices.ForEach(c => meshTemplate.Vertices[c] = Vector3.down*4);
            //meldVerticesList.MeldVertices[8].Vertices.ForEach(c => meshTemplate.Vertices[c] = Vector3.left*4);

            foreach (var a1 in branchesVertLists.Skip(1))
            {
                foreach (var vx in a1.SelectMany(c => c.Vertices))
                {
                    meshTemplate.Vertices[vx] = Vector3.back;
                }
            }

            CreateDebugTreeObject(meshTemplate);
        }

        private int treeNo = 0;
        private void CreateDebugTreeObject(MeshTemplate meshTemplate)
        {
            var mesh = new Mesh();
            meshTemplate.Fill(mesh);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "DebugTree"+treeNo;
            go.GetComponent<MeshFilter>().mesh = mesh;

            treeNo++;
        }


        private class MeshTemplate
        {
            private List<Vector3> _vertices;
            private List<int> _indices;
            private List<Vector2> _uvs;

            private List<List<int>> _vertexToTriangles;

            public MeshTemplate(List<Vector3> vertices, List<int> indices, List<Vector2> uvs)
            {
                _vertices = vertices;
                _indices = indices;
                _uvs = uvs;

                _vertexToTriangles = new List<List<int>>();
                for (int i = 0; i < _vertices.Count; i++)
                {
                    _vertexToTriangles.Add(new List<int>());
                }

                for (int i = 0; i < _indices.Count/3; i++)
                {
                    var t0 = _indices[i * 3 + 0];
                    var t1 = _indices[i * 3 + 1];
                    var t2 = _indices[i * 3 + 2];

                    _vertexToTriangles[t0].Add(i);
                    _vertexToTriangles[t1].Add(i);
                    _vertexToTriangles[t2].Add(i);
                }
            }

            public void Fill(Mesh mesh)
            {
                mesh.SetVertices(Vertices);
                mesh.SetIndices(Indices.ToArray(), MeshTopology.Triangles, 0);
                mesh.SetUVs(0, Uvs);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
            }

            public MeldVertexList CreateMeldVerticesList()
            {
                var posDict = new Dictionary<Vector3, List<int>>();

                for (int i = 0; i < Vertices.Count; i++)
                {
                    var pos = Vertices[i];
                    pos = new Vector3(
                        Mathf.Round(pos.x * 10000)/10000f, // todo parametrize 10000
                        Mathf.Round(pos.y * 10000)/10000f,
                        Mathf.Round(pos.z * 10000)/10000f
                        );

                    if (!posDict.ContainsKey(pos))
                    {
                        posDict.Add(pos, new List<int>());
                    }
                    posDict[pos].Add(i);
                }

                var meldVertices = posDict.Values.Select((c,i) => new MeldVertex(c,i)).ToList();
                return new MeldVertexList(meldVertices, this);
            }

            public List<Vector3> Vertices => _vertices;
            public List<int> Indices => _indices;
            public List<Vector2> Uvs => _uvs;

            public List<int> GetTriangleIndexOfVertex(int i)
            {
                var toReturn = _vertexToTriangles[i];
                return toReturn;
            }

            public List<int> GetVerticesInTriangle(int i)
            {
                return new List<int>()
                {
                    _indices[i*3 + 0],
                    _indices[i*3 + 1],
                    _indices[i*3 + 2],
                };
            }
        }

        private class MeldVertex
        {
            private List<int> _vertices;
            private int _id;

            public MeldVertex(List<int> vertices, int id)
            {
                _vertices = vertices;
                _id = id;
            }

            public List<int> Vertices => _vertices;
            public int Id => _id;
        }

        private class MeldVertexList
        {
            private MeshTemplate _meshTemplate;
            private List<MeldVertex> _meldVertices;
            private List<MeldVertex> _nativeToMeldList;

            public MeldVertexList(List<MeldVertex> meldVertices, MeshTemplate meshTemplate)
            {
                _meldVertices = meldVertices;
                _meshTemplate = meshTemplate;
                _nativeToMeldList = Enumerable.Repeat<MeldVertex>(null, meshTemplate.Vertices.Count).ToList();
                foreach (var aMeldVertex in meldVertices)
                {
                    foreach (var nativeVertexIndex in aMeldVertex.Vertices)
                    {
                        _nativeToMeldList[nativeVertexIndex] = aMeldVertex;
                    }
                }
            }

            public List<MeldVertex> MeldVertices => _meldVertices;
            public int Count => _meldVertices.Count;

            public List<MeldVertex> NeighboursOf(MeldVertex vertex)
            {
                var nativeIndexes = vertex.Vertices;
                var vertexesInTriangle = nativeIndexes
                    .SelectMany(c => _meshTemplate.GetTriangleIndexOfVertex(c))
                    .Distinct()
                    .SelectMany(c => _meshTemplate.GetVerticesInTriangle(c))
                    .Distinct()
                    .Where(c => !nativeIndexes.Contains(c))
                    .Select(c => _nativeToMeldList[c]).Distinct().ToList();

                return vertexesInTriangle;
            }
        }

        private Branch ExtractBranch(List<MeldVertex> vertexList, MeldVertexList wholeList)
        {
            var circles = new List<Circle>();

            var previousCircle = new List<MeldVertex>();
            var currentCircle = vertexList.Where(c => wholeList.NeighboursOf(c).Count < 5).OrderByDescending(c => c.Id)
                .Skip(1).ToList();

            int i = 0;
            while (currentCircle.Any())
            {
                circles.Add(new Circle(currentCircle));
                var newCircle = currentCircle.SelectMany(wholeList.NeighboursOf).Distinct()
                    .Where(c => !currentCircle.Contains(c))
                    .Where(c => !previousCircle.Contains(c))
                    .ToList();
                previousCircle = currentCircle;
                currentCircle = newCircle;
                Preconditions.Assert(i++ < 50, "E50. Problem. RotatingNeighbours");
            }

            return new Branch(circles);
        }

        private class Branch
        {
            private List<Circle> _circles;

            public Branch(List<Circle> circles)
            {
                _circles = circles;
            }

            public List<Circle> Circles => _circles;
        }

        private class Circle
        {
            private List<MeldVertex> _vertices;

            public Circle(List<MeldVertex> vertices)
            {
                _vertices = vertices;
            }

            public List<MeldVertex> Vertices => _vertices;
        }
    }
}
