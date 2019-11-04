using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR
{
    public  class NPRAdjacencyBufferDebugGO : MonoBehaviour
    {
        private Action _resetingAction;

        public void Start()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            GetComponent<MeshFilter>().mesh = mesh;
            var generator = new NPRAdjacencyBufferGenerator();
            var buffer = generator.GenerateTriangleAdjacencyBuffer(mesh);

            var computeBuffer = new ComputeBuffer(buffer.Length, sizeof(float) * 3, ComputeBufferType.Default);
            computeBuffer.SetData(buffer);

            _resetingAction = () => GetComponent<MeshRenderer>().material.SetBuffer("_AdjacencyBuffer", computeBuffer);
            Reset();

            InvokeRepeating("Reset", 1, 3);
        }

        public void Reset()
        {
            if (_resetingAction != null)
            {
                _resetingAction();
            }
        }
    }
}
