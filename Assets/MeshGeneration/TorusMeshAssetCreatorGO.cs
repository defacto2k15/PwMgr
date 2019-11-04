using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.MeshGeneration
{
    public class TorusMeshAssetCreatorGO : MonoBehaviour
    {
        public string Path;
        public bool WriteToFile = false;
        public float Radius1 = 1f;
        public float Radius2 = .3f;
        public int RadSeg = 24;
        public int Sides = 18;

        public void Start()
        {
            var mesh = TorusGenerator.GenerateTorus(Radius1, Radius2, RadSeg, Sides);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.GetComponent<MeshFilter>().mesh = mesh;
            if (WriteToFile)
            {
                MyAssetDatabase.CreateAndSaveAsset(mesh, Path);
            }
        }
    }

    public static class TorusGenerator
    {
        public static Mesh GenerateTorus(float radius1, float radius2, int radSeg, int sides)
        {
            Mesh mesh = new Mesh();
            mesh.Clear();

            #region Vertices		

            Vector3[] vertices = new Vector3[(radSeg + 1) * (sides + 1)];
            float _2pi = Mathf.PI * 2f;
            for (int seg = 0; seg <= radSeg; seg++)
            {
                int currSeg = seg == radSeg ? 0 : seg;

                float t1 = (float) currSeg / radSeg * _2pi;
                Vector3 r1 = new Vector3(Mathf.Cos(t1) * radius1, 0f, Mathf.Sin(t1) * radius1);

                for (int side = 0; side <= sides; side++)
                {
                    int currSide = side == sides ? 0 : side;

                    Vector3 normale = Vector3.Cross(r1, Vector3.up);
                    float t2 = (float) currSide / sides * _2pi;
                    Vector3 r2 = Quaternion.AngleAxis(-t1 * Mathf.Rad2Deg, Vector3.up) * new Vector3(Mathf.Sin(t2) * radius2, Mathf.Cos(t2) * radius2);

                    vertices[side + seg * (sides + 1)] = r1 + r2;
                }
            }

            #endregion

            #region Normales		

            Vector3[] normales = new Vector3[vertices.Length];
            for (int seg = 0; seg <= radSeg; seg++)
            {
                int currSeg = seg == radSeg ? 0 : seg;

                float t1 = (float) currSeg / radSeg * _2pi;
                Vector3 r1 = new Vector3(Mathf.Cos(t1) * radius1, 0f, Mathf.Sin(t1) * radius1);

                for (int side = 0; side <= sides; side++)
                {
                    normales[side + seg * (sides + 1)] = (vertices[side + seg * (sides + 1)] - r1).normalized;
                }
            }

            #endregion

            #region UVs

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int seg = 0; seg <= radSeg; seg++)
            for (int side = 0; side <= sides; side++)
                uvs[side + seg * (sides + 1)] = new Vector2((float) seg / radSeg, (float) side / sides);

            #endregion

            #region Triangles

            int nbFaces = vertices.Length;
            int nbTriangles = nbFaces * 2;
            int nbIndexes = nbTriangles * 3;
            int[] triangles = new int[nbIndexes];

            int i = 0;
            for (int seg = 0; seg <= radSeg; seg++)
            {
                for (int side = 0; side <= sides - 1; side++)
                {
                    int current = side + seg * (sides + 1);
                    int next = side + (seg < (radSeg) ? (seg + 1) * (sides + 1) : 0);

                    if (i < triangles.Length - 6)
                    {
                        triangles[i++] = current;
                        triangles[i++] = next;
                        triangles[i++] = next + 1;

                        triangles[i++] = current;
                        triangles[i++] = next + 1;
                        triangles[i++] = current + 1;
                    }
                }
            }

            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            return mesh;
        }

    }

    public static class IcoSphereGenerator
    {
        
    }
}
