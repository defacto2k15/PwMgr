using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEditor;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    public class NprUniqueVertexArrayAssetGeneratorOC : MonoBehaviour
    {
        public string TargetPath;
        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var generator = new UniqueVertexArrayBufferGenerator("npr_uniqueVertexGenerator_comp", new UnityThreadComputeShaderExecutorObject());
            var mesh = GetComponent<MeshFilter>().mesh;
            var verticesArray = generator.Generate(mesh).Result;

            var newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            newMesh.vertices = Enumerable.Range(0, mesh.triangles.Length )
                .Select(c => new Vector3(verticesArray[c * 3 + 0], verticesArray[c * 3 + 1], verticesArray[c * 3 + 2])).ToArray();

            newMesh.triangles = Enumerable.Range( 0, mesh.triangles.Length).ToArray();
            newMesh.RecalculateNormals();
            newMesh.RecalculateTangents();
            newMesh.RecalculateBounds();

            MyAssetDatabase.CreateAndSaveAsset(newMesh, TargetPath);
            //GetComponent<MeshFilter>().mesh = newMesh;
            //Debug.Log("Created asset at "+TargetPath);
        }
    }

    public class UniqueVertexArrayBufferGenerator
    {
        private readonly string _shaderName;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public UniqueVertexArrayBufferGenerator(String shaderName, UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _shaderName = shaderName;
            _shaderExecutorObject = shaderExecutorObject;
        }

        public async Task<float[]> Generate(Mesh mesh)
        {
            var generator = new BufferGeneratorUsingComputeShader(_shaderName, _shaderExecutorObject);
            var array = await generator.Generate(mesh, "OutVertices", 3, mesh.triangles.Length, meshMustbeUniquefied:false);
            return array;
        }
    }
}
