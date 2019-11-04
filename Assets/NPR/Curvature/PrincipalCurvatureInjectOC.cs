using System;
using System.Collections.Generic;
using System.Linq;
using Assets.NPR.Curvature;
using UnityEngine;

namespace Assets.NPR
{
    public class PrincipalCurvatureInjectOC : MonoBehaviour
    {
        public MeshCurvatureDetailSE CurvatureDetail;

        public void Start()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            ResetMeshDetails(mesh);
        }

        public void OnValidate()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            ResetMeshDetails(mesh);
        }

        public void OnEnable()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            ResetMeshDetails(mesh);
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

        public void ResetMeshDetails(Mesh mesh)
        {
            if (CurvatureDetail != null)
            {
                var globalMax = Mathf.Max(
                    CurvatureDetail.PrincipalValue1.Max(),
                    CurvatureDetail.PrincipalValue2.Max());

                var globalMin = Mathf.Min(
                    CurvatureDetail.PrincipalValue1.Min(),
                    CurvatureDetail.PrincipalValue2.Min());
                var globalExtreme = Mathf.Max(Mathf.Abs(globalMax), Mathf.Abs(globalMin));


                mesh.SetUVs(1, CurvatureDetail.PrincipalDirection1
                    .Select((c, i) => new Vector4(c.x, c.y, c.z, tanhRemap(globalExtreme, CurvatureDetail.PrincipalValue1[i])))
                    .ToList());

                mesh.SetUVs(2, CurvatureDetail.PrincipalDirection2
                    .Select((c, i) => new Vector4(c.x, c.y, c.z, tanhRemap(globalExtreme, CurvatureDetail.PrincipalValue2[i])))
                    .ToList());
            }
            else
            {
                mesh.SetUVs(1, new List<Vector3>());
                mesh.SetUVs(2, new List<Vector3>());
            }
        }
    }
}