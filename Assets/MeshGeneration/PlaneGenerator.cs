using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.MeshGeneration
{
    class PlaneGenerator
    {
        public static void createPlaneMeshFilter(MeshFilter filter, float length, float width, float[,] heightArray)
        {
            Mesh mesh = filter.mesh;
            mesh.Clear();

            int resX = heightArray.GetLength(0);
            int resZ = heightArray.GetLength(1);

            #region Vertices 

            //todo remove regions to refactor to methods when resharper is present!
            Vector3[] vertices = new Vector3[resX * resZ];
            for (int z = 0; z < resZ; z++)
            {
                // [ -length / 2, length / 2 ]
                float zPos = ((float) z / (resZ - 1)) * length;
                for (int x = 0; x < resX; x++)
                {
                    // [ -width / 2, width / 2 ]
                    float xPos = ((float) x / (resX - 1)) * width;
                    vertices[x + z * resX] = new Vector3(xPos, heightArray[x, z], zPos);
                }
            }

            Vector3[] normales = new Vector3[vertices.Length];
            for (int n = 0; n < normales.Length; n++)
                normales[n] = Vector3.up;

            #endregion

            #region UVs

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int v = 0; v < resZ; v++)
            {
                for (int u = 0; u < resX; u++)
                {
                    uvs[u + v * resX] = new Vector2((float) u / (resX - 1), (float) v / (resZ - 1));
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
        }

        public static Mesh CreatePlaneMesh(float length, float width, float[,] heightArray)
        {
            Mesh mesh = new Mesh();
            mesh.Clear();

            int resX = heightArray.GetLength(0);
            int resZ = heightArray.GetLength(1);

            #region Vertices 

            //todo remove regions to refactor to methods when resharper is present!
            Vector3[] vertices = new Vector3[resX * resZ];
            for (int z = 0; z < resZ; z++)
            {
                // [ -length / 2, length / 2 ]
                float zPos = ((float) z / (resZ - 1)) * length;
                for (int x = 0; x < resX; x++)
                {
                    // [ -width / 2, width / 2 ]
                    float xPos = ((float) x / (resX - 1)) * width;
                    vertices[x + z * resX] = new Vector3(xPos, heightArray[x, z], zPos);
                }
            }

            Vector3[] normales = new Vector3[vertices.Length];
            for (int n = 0; n < normales.Length; n++)
                normales[n] = Vector3.up;

            #endregion

            #region UVs

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int v = 0; v < resZ; v++)
            {
                for (int u = 0; u < resX; u++)
                {
                    uvs[u + v * resX] = new Vector2((float) u / (resX - 1), (float) v / (resZ - 1));
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
        } //todo refactor, there is repetition with that one up

        private static Dictionary<IntVector2, Mesh> _planeCache = new Dictionary<IntVector2, Mesh>();
        private static int triCount = 0;

        public static Mesh CreateFlatPlaneMesh(int resX, int resZ)
        {
            triCount += (resX * resZ) * 2;
            var resolution = new IntVector2(resX, resZ);
            if (_planeCache.ContainsKey(resolution))
            {
                return _planeCache[resolution];
            }
            else
            {
                Mesh mesh = new Mesh();
                mesh.Clear();

                float length = 1f;
                float width = 1f;

                #region Vertices 

                //todo remove regions to refactor to methods when resharper is present!
                Vector3[] vertices = new Vector3[resX * resZ];
                for (int z = 0; z < resZ; z++)
                {
                    // [ -length / 2, length / 2 ]
                    float zPos = ((float) z / (resZ - 1)) * length;
                    for (int x = 0; x < resX; x++)
                    {
                        // [ -width / 2, width / 2 ]
                        float xPos = ((float) x / (resX - 1)) * width;
                        vertices[x + z * resX] = new Vector3(xPos, 0, zPos);
                    }
                }

                Vector3[] normales = new Vector3[vertices.Length];
                for (int n = 0; n < normales.Length; n++)
                    normales[n] = Vector3.up;

                #endregion

                #region UVs

                Vector2[] uvs = new Vector2[vertices.Length];
                for (int v = 0; v < resZ; v++)
                {
                    for (int u = 0; u < resX; u++)
                    {
                        uvs[u + v * resX] = new Vector2((float) u / (resX - 1), (float) v / (resZ - 1));
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

                mesh.bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1, 1, 1));
                //mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                _planeCache[resolution] = mesh;
                return mesh;
            }
        }


        private static Dictionary<IntVector2, Mesh> _marginedTerrainCache = new Dictionary<IntVector2, Mesh>();

        public static Mesh CreateMarginedTerrainPlaneMesh(int nonMarginedResX, int nonMarginedResZ)
        {
            int resX = nonMarginedResX + 1;
            int resZ = nonMarginedResZ + 1;
            var resolution = new IntVector2(resX, resZ);
            if (_marginedTerrainCache.ContainsKey(resolution))
            {
                return _marginedTerrainCache[resolution];
            }
            else
            {
                Mesh mesh = new Mesh();
                mesh.Clear();

                float length = 1f;
                float width = 1f;

                #region Vertices 

                //todo remove regions to refactor to methods when resharper is present!
                Vector3[] vertices = new Vector3[resX * resZ];
                for (int z = 0; z < resZ; z++)
                {
                    // [ -length / 2, length / 2 ]
                    float zPos = ((float) z / (resZ - 1)) * length;
                    for (int x = 0; x < resX; x++)
                    {
                        // [ -width / 2, width / 2 ]
                        float xPos = ((float) x / (resX - 1)) * width;
                        vertices[x + z * resX] = new Vector3(xPos, 0, zPos);
                    }
                }

                Vector3[] normales = new Vector3[vertices.Length];
                for (int n = 0; n < normales.Length; n++)
                    normales[n] = Vector3.up;

                #endregion

                #region UVs

                Vector2[] uvs = new Vector2[vertices.Length];
                for (int v = 0; v < resZ; v++)
                {
                    for (int u = 0; u < resX; u++)
                    {
                        var uv = new Vector2((float) u / (resX - 1 - 1), (float) v / (resZ - 1 - 1));
                        if (v == resZ - 1) // margin
                        {
                            uv.y = 2;
                        }
                        if (u == resX - 1) //margin
                        {
                            uv.x = 2;
                        }
                        uvs[u + v * resX] = uv;
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

                mesh.bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1, 1, 1));
                //mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                _planeCache[resolution] = mesh;
                return mesh;
            }
        }

        public static Mesh CreateETerrainSegmentMesh(int nonMarginedResX, int nonMarginedResZ)
        {
            int resX = nonMarginedResX + 1;
            int resZ = nonMarginedResZ + 1;
            var resolution = new IntVector2(resX, resZ);
            if (_marginedTerrainCache.ContainsKey(resolution))
            {
                return _marginedTerrainCache[resolution];
            }
            else
            {
                Mesh mesh = new Mesh();
                mesh.Clear();

                float length = 1f;
                float width = 1f;

                #region Vertices 

                //todo remove regions to refactor to methods when resharper is present!
                Vector3[] vertices = new Vector3[resX * resZ];
                for (int z = 0; z < resZ; z++)
                {
                    // [ -length / 2, length / 2 ]
                    float zPos = ((float) z / (resZ - 1)) * length;
                    for (int x = 0; x < resX; x++)
                    {
                        // [ -width / 2, width / 2 ]
                        float xPos = ((float) x / (resX - 1)) * width;
                        vertices[x + z * resX] = new Vector3(xPos, 0, zPos);
                    }
                }

                Vector3[] normales = new Vector3[vertices.Length];
                for (int n = 0; n < normales.Length; n++)
                    normales[n] = Vector3.up;

                #endregion

                #region UVs

                Vector2[] uvs = new Vector2[vertices.Length];
                for (int v = 0; v < resZ; v++)
                {
                    for (int u = 0; u < resX; u++)
                    {
                        var uv = new Vector2((float) u / (resX - 1), (float) v / (resZ - 1));
                        uvs[u + v * resX] = uv;
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

                mesh.bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1, 1, 1));
                //mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                _planeCache[resolution] = mesh;
                return mesh;
            }
        }
    }
}