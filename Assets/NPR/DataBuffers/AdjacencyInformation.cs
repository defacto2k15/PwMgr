using System;
using UnityEngine;

namespace Assets.NPR
{
    public class AdjacencyInformation
    {
        private Vector3[] _data = new Vector3[3*2];

        public void SetPosition(int vertexIndex, Vector3 position)
        {
            _data[vertexIndex ] = position;
        }

        public void SetNormal(int vertexIndex, Vector3 normal)
        {
            _data[3 + vertexIndex] = normal;
        }

        public void WriteToBuffer(Vector3[] buffer, int triangleIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                buffer[triangleIndex * 6 + i] = _data[i];
            }
        }
    }
}