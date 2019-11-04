using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.MeshGeneration;
using UnityEngine;

namespace Assets.Utils
{
    public class DebugMeshGeneratorGO : MonoBehaviour
    {
        public string FilePath;

        public void Start()
        {
            MyAssetDatabase.CreateAndSaveAsset(GenerateSawtoothMesh(), FilePath);
        }

        private Mesh GenerateSawtoothMesh()
        {
            var length = 50;
            float[,] heightArray = new float[length, length];
            for (int x = 0; x < length; x++)
            {
                var height = Sawtooth(x, 10) * 0.01f;
                for (int y = 0; y < length; y++)
                {
                    heightArray[x, y] = height;
                }
            }

            var mesh = PlaneGenerator.CreatePlaneMesh(1, 1, heightArray);
            return mesh;
        }

        private float Sawtooth(int inputPosition, int period)
        {
            return (float) (period) / 2 - Math.Abs(inputPosition % period - period/2);
        }
    }
}