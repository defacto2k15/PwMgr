using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.Curvature
{
    public class DebugDrawPrincipalCurvaturesGO : MonoBehaviour
    {
        public MeshCurvatureDetailSE CurvatureDetail;
        public Vector3[] verticles;
        public float LinesLength;

        public void Start()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            verticles = mesh.vertices;
        }

        public void OnValidate()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            verticles = mesh.vertices;
        }

        public void OnEnable()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            verticles = mesh.vertices;
        }

        void FixedUpdate()
        {
            if (CurvatureDetail != null)
            {
                if (CurvatureDetail.PrincipalDirection1.Length != verticles.Length)
                {
                    Debug.LogError("E881 Mismatch in principal directions and verticles length");
                }
                else
                {
                    Color color = Color.blue;

                    for (int i = 0; i < verticles.Length; i++)
                    {

                    }
                }
            }
        }
    }
}
