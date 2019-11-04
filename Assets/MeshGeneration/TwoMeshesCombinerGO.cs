using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.MeshGeneration
{
    public class TwoMeshesCombinerGO : MonoBehaviour
    {
        public Mesh Mesh1;
        public Mesh Mesh2;
        public string OutPath;

        public void Start()
        {
            var newMesh = CombineMeshes(Mesh1, Mesh2);

            MyAssetDatabase.CreateAndSaveAsset(newMesh, OutPath);
        }

        public static Mesh CombineMeshes(Mesh m1, Mesh m2)
        {
            var newVertices = new Vector3[m1.vertices.Length + m2.vertices.Length];
            var newTris = new int[m1.triangles.Length + m2.triangles.Length];
            var newNormals = new Vector3[m1.normals.Length + m2.normals.Length];
            var newTangents = new Vector4[m1.tangents.Length + m2.tangents.Length];
            var newUvs = new Vector2[m1.uv.Length + m2.uv.Length];

            Array.Copy(m1.vertices, 0, newVertices, 0, m1.vertices.Length);
            Array.Copy(m2.vertices, 0, newVertices, m1.vertices.Length, m2.vertices.Length);

            Array.Copy(m1.triangles, 0, newTris, 0, m1.triangles.Length);
            Array.Copy(m2.triangles.Select(c => c + m1.vertices.Length).ToArray(), 0, newTris, m1.triangles.Length, m2.triangles.Length);

            Array.Copy(m1.normals, 0, newNormals, 0, m1.normals.Length);
            Array.Copy(m2.normals, 0, newNormals, m1.normals.Length, m2.normals.Length);

            Array.Copy(m1.tangents, 0, newTangents, 0, m1.tangents.Length);
            Array.Copy(m2.tangents, 0, newTangents, m1.tangents.Length, m2.tangents.Length);

            Array.Copy(m1.uv.Select(c => c*0.5f).ToArray(), 0, newUvs, 0, m1.uv.Length  );
            Array.Copy(m2.uv.Select(c => new Vector2(0.5f,0.5f) + c*0.5f).ToArray(), 0, newUvs, m1.uv.Length, m2.uv.Length  );

            var newMesh = new Mesh()
            {
                vertices = newVertices,
                triangles = newTris,
                normals = newNormals,
                tangents = newTangents,
                uv = newUvs
            };
            newMesh.RecalculateBounds();
            return newMesh;
        }
    }
}
