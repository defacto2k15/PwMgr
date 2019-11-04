using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.NPR.Lines;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.NPR.DataBuffers
{
    public class NprInterpolatedNormalsAssetGenerator : INprShaderBufferAssetGenerator
    {
        public ComputeBuffer CreateAndSave(string path, Mesh mesh, Dictionary<MyShaderBufferType, ComputeBuffer> createdBuffers)
        {
            var generator = new NPRInterpolatedNormalsBufferGenerator();

            var array = generator.Generate(mesh);

            var buffer = ScriptableObject.CreateInstance<ShaderBufferSE>();
            buffer.Data = array.SelectMany(c => c.ToArray()).ToArray();
            MyAssetDatabase.CreateAndSaveAsset(buffer, path);

            var floatsPerVertex = (3);
            var computeBuffer = new ComputeBuffer(array.Length, sizeof(float) * floatsPerVertex);
            computeBuffer.SetData(buffer.Data);
            return computeBuffer;
        }

        public List<MyShaderBufferType> RequiredBuffers => new List<MyShaderBufferType>();
    }

    public class NPRInterpolatedNormalsBufferGenerator
    {
        public Vector3[] Generate(Mesh mesh)
        {
            UnaliasedMeshGenerator generator = new UnaliasedMeshGenerator();
            var unaliasedMesh =  generator.GenerateUnaliasedMesh(mesh);
            var unityUnaliasedMesh = new Mesh();
            unityUnaliasedMesh.indexFormat = IndexFormat.UInt32;
            unityUnaliasedMesh.vertices = unaliasedMesh.Vertices;
            unityUnaliasedMesh.triangles = unaliasedMesh.Triangles;
            unityUnaliasedMesh.RecalculateNormals();

            var normalsArray = new Vector3[mesh.normals.Length];
            var unityUnaliazedMeshNormals = unityUnaliasedMesh.normals.ToArray();
            Parallel.For(0, unaliasedMesh.OriginalIndexToIndex.Length, (originalIndex) =>
            {
                var newIndex = unaliasedMesh.OriginalIndexToIndex[originalIndex];
                normalsArray[originalIndex] = unityUnaliazedMeshNormals[newIndex];
            });
            return normalsArray;
        }
    }
}