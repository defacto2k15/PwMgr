using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.NPR.DataBuffers;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Lines
{
    public class NPRAdjacencyAssetGeneratorGO : MonoBehaviour
    {
        public string DestinationPath = "Assets/NPRResources/Curvature/VenusMeshCurvatureDetail.asset";

        public void Start()
        {
            var generator = new NPRAdjacencyAssetGenerator();
            var mesh = GetComponent<MeshFilter>().mesh;
            generator.CreateAndSave(DestinationPath, mesh, new Dictionary<MyShaderBufferType, ComputeBuffer>());
        }
    }

    public class NPRAdjacencyAssetGenerator : INprShaderBufferAssetGenerator
    {
        public ComputeBuffer CreateAndSave(string path, Mesh mesh, Dictionary<MyShaderBufferType, ComputeBuffer> createdBuffers)
        {
            var generator = new NPRAdjacencyBufferGenerator();
            var detail = generator.GenerateTriangleAdjacencyBuffer(mesh);
            var buffer = ScriptableObject.CreateInstance<ShaderBufferSE>();
            buffer.Data = detail.SelectMany(c => c.ToArray()).ToArray();
            MyAssetDatabase.CreateAndSaveAsset(buffer, path);

            var computeBuffer = new ComputeBuffer(detail.Length, sizeof(float) * 3);
            computeBuffer.SetData(buffer.Data);
            return computeBuffer;
        }

        public List<MyShaderBufferType> RequiredBuffers  => new List<MyShaderBufferType>();
    }
}
