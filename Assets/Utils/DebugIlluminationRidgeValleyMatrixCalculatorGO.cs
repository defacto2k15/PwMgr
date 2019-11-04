using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace Assets.Utils
{
    public class DebugIlluminationRidgeValleyMatrixCalculatorGO : MonoBehaviour
    {
        public int Radius;
        public int StepSizeInPixels;

        public void Calculate()
        {
            int N = (int) Math.Pow(2 * Radius + 1, 2);
            //float[,] X = new float[N,6];
            var X = Matrix<double>.Build.Dense(N, 6);


            int i = 0;
            var sb = new StringBuilder();

            sb.Append("{");
            for (int x = -Radius; x <= Radius; x++)
            {
                for (int y = -Radius; y <= Radius; y++)
                {
                    float fx = x * StepSizeInPixels;
                    float fy = y * StepSizeInPixels;

                    X[i,0] = fx * fx;
                    X[i,1] = 2 * fx * fy;
                    X[i,2] = fy * fy;
                    X[i,3] = fx;
                    X[i,4] = fy;
                    X[i,5] = 1;

                    sb.Append("{");
                    for (int r = 0; r < 6; r++)
                    {
                        sb.Append(X[i, r] + ",");
                    }
                    sb.Append("},");

                    i++;
                }

            }
            sb.Append("}");
            sb.Replace(",}", "}");
            //Debug.Log(sb.ToString());

            var H = X.Transpose().Multiply(X).Inverse().Multiply(X.Transpose());

            sb = new StringBuilder();
            for (int j = 0; j < H.RowCount; j++)
            {
                for (int k = 0; k < H.ColumnCount; k++)
                {
                    sb.Append($"H[{j}][{k}]={H[j,k]};\n");
                } 
            }
            Debug.Log(sb.ToString());

        }
    }
}
