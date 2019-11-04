using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Curvature
{
    public class DebugArapUV : MonoBehaviour
    {
        public void Start()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            var unaliasedMeshGenerator = new UnaliasedMeshGenerator();
            var unaliasedMesh = unaliasedMeshGenerator.GenerateUnaliasedMesh(mesh);

            var arapUvGenerator = new ArapUvGenerator();
            var uvs = arapUvGenerator.GenerateUv(unaliasedMesh);

            mesh.SetUVs(0,uvs);
        }

    }

    public class ArapUvGenerator
    {
        public List<Vector2> GenerateUv(UnaliasedMesh mesh)
        {
            var flatVerticlesArray = mesh.Vertices.SelectMany(c => c.ToArray()).ToArray();
            var verticlesCount = mesh.Vertices.Length;
            var outUvsArray = new float[verticlesCount * 2];

            var result = PrincipalCurvatureDll.compute_arapUv(flatVerticlesArray, verticlesCount, mesh.Triangles, mesh.Triangles.Length/3, outUvsArray, 100);
            Preconditions.Assert(result==0, "Calling compute_arapUv failed, as returned status "+result);

            var uvsAsVectors = new Vector2[verticlesCount];
            for (int i = 0; i < verticlesCount; i++)
            {
                uvsAsVectors[i] = new Vector2(outUvsArray[i * 2 + 0], outUvsArray[i*2 + 1]);
            }

            return mesh.OriginalIndexToIndex.Select(c => uvsAsVectors[c]).ToList();
        }
    }
}
