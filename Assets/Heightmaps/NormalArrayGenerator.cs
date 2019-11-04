using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Heightmaps
{
    class NormalArrayGenerator
    {
        public NormalArray GenerateNormalArray(HeightmapArray heightmapArray, float heightMultiplier)
        {
            var heightArray = heightmapArray.HeightmapAsArray;
            Vector3[,] outArray = new Vector3[heightmapArray.Width, heightmapArray.Height];
            for (int x = 0; x < heightmapArray.Width; x++)
            {
                for (int y = 0; y < heightmapArray.Height; y++)
                {
                    outArray[x, y] =
                        CalculateNormalsAverage(FindNeighbourVectorDiffrences(heightArray, x, y, heightMultiplier));
                }
            }
            return new NormalArray(outArray);
        }

        private Vector3 CalculateNormalsAverage(Vector3[] diffrencesArray)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                int i1 = i;
                int i2 = (i + 1) % 6;
//                if (!(diffrencesArray[i1] == Vector3.zero || diffrencesArray[i2] == Vector3.zero))
//                {
                var v1 = Vector3.Normalize(Vector3.Cross(diffrencesArray[i1], diffrencesArray[i2]));
                sum += v1;
//                }
            }
            return Vector3.Normalize(sum);
        }

        // allways returns 6-element array.
        // if element cannot be created, returns zero
        private Vector3[] FindNeighbourVectorDiffrences(float[,] heightArray, int x, int y, float heightMultiplier)
        {
            Vector3[] outArray = new Vector3[6];
            Vector3 c = Pos(x, y, heightArray, heightMultiplier);

            outArray[0] = TryGetPos(x, y - 1, heightArray, heightMultiplier) - c;
            outArray[1] = TryGetPos(x + 1, y, heightArray, heightMultiplier) - c;
            outArray[2] = TryGetPos(x + 1, y + 1, heightArray, heightMultiplier) - c;
            outArray[3] = TryGetPos(x, y + 1, heightArray, heightMultiplier) - c;
            outArray[4] = TryGetPos(x - 1, y, heightArray, heightMultiplier) - c;
            outArray[5] = TryGetPos(x - 1, y - 1, heightArray, heightMultiplier) - c;

            return outArray;
        }

        private Vector3 TryGetPos(int x, int y, float[,] inArray, float heightMultiplier)
        {
            if (x < 0 || y < 0)
            {
                return Vector3.zero;
            }
            if (x >= inArray.GetLength(0) || y >= inArray.GetLength(1))
            {
                return Vector3.zero;
            }
            return Pos(x, y, inArray, heightMultiplier);
        }

        private Vector3 Pos(int x, int y, float[,] inArray, float heightMultiplier)
        {
            return new Vector3(x, inArray[x, y] * heightMultiplier, y);
        }
    }
}