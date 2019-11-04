using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.MeshGeneration;
using UnityEngine;

namespace Assets.Utils
{
    public class MeshTangentRecalculatorOC : MonoBehaviour
    {
        public void Start()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            //MeshGeneration.calculateMeshTangents(mesh);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }
    }
}
