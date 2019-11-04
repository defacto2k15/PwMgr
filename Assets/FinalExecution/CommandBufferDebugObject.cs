using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.FinalExecution
{
    public class CommandBufferDebugObject : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _material;
        private List<List<Matrix4x4>> _maticesArrays;
        private MaterialPropertyBlock _properties;
        private CommandBuffer _commandBuffer;


        public void Start()
        {
            _mesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().mesh;

            var shader = Shader.Find("Standard");
            _material = new Material(shader);
            _material.enableInstancing = true;

            _maticesArrays = new List<List<Matrix4x4>>();
            for (int i = 0; i < 50; i++)
            {
                for (int x = 0; x < 1000; x += 35)
                {
                    var array = new List<Matrix4x4>();
                    for (int y = 0; y < 1000; y += 35)
                    {
                        array.Add(Matrix4x4.TRS(new Vector3(x, i*15, y), Quaternion.Euler(Vector3.zero), Vector3.one * 4));
                    }
                    _maticesArrays.Add(array);
                }
            }

            _properties = new MaterialPropertyBlock();

            //_commandBuffer = new CommandBuffer();
            //_commandBuffer.DrawMeshInstanced(_mesh, 0, _material, 0, _maticesArray.ToArray(), _maticesArray.Count);
            //Camera.main.AddCommandBuffer(CameraEvent.AfterGBuffer, _commandBuffer);
        }

        public void Update()
        {
            foreach (var arr in _maticesArrays)
            {
                Graphics.DrawMeshInstanced(_mesh, 0, _material, arr, _properties, ShadowCastingMode.Off);
            }

            //Graphics.ExecuteCommandBuffer(_commandBuffer);

        }
    }
}
