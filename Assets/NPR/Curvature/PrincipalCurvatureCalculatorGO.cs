using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.MeshGeneration;
using Assets.NPR.Curvature;
using Assets.Utils;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace Assets.NPR
{
    public class PrincipalCurvatureCalculatorGO : MonoBehaviour
    {
        public string DestinationPath;
        // "Assets/NPRResources/Curvature/VenusMeshCurvatureDetail.asset")
        public void Start()
        {
            var unaliasedMeshGenerator = new UnaliasedMeshGenerator();
            MeshCurvatureAssetGenerator.CreateAndSave(unaliasedMeshGenerator.GenerateUnaliasedMesh(GetComponent<MeshFilter>().mesh),  DestinationPath);
        }

        public void Start3()
        {
            var length = 50;
            float[,] heightArray = new float[length,length];
            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    var ax = x / (float)length * 2 * Mathf.PI;
                    var ay = y / (float)length * 2 * Mathf.PI;

                    heightArray[x,y] = Mathf.Sin(ax )/10;
                }
            }
            var mesh = PlaneGenerator.CreatePlaneMesh(1, 1, heightArray);
            MyAssetDatabase.CreateAndSaveAsset(mesh, "Assets/NPRResources/SimpleMesh1.asset");
        }
    }
}