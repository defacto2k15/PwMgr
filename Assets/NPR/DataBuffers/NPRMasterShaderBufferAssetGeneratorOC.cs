using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.NPR.Lines;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.NPR.DataBuffers
{
    public class NPRMasterShaderBufferAssetGeneratorOC : MonoBehaviour
    {
        public List<ShaderBufferGenerationOrders> Orders = new List<ShaderBufferGenerationOrders>();
        private string _pathTemplate = "Assets/NPRResources/BuffersCache/{0}.{1}.asset";

        private Dictionary<MyShaderBufferType, INprShaderBufferAssetGenerator> _generatorsDict = new Dictionary<MyShaderBufferType, INprShaderBufferAssetGenerator>()
        {
            {MyShaderBufferType.Adjacency, new NPRAdjacencyAssetGenerator() },
            {MyShaderBufferType.Barycentric, new NprBarycentricCoordsAssetGenerator() },
            {MyShaderBufferType.EdgeAngle, new EdgeAngleAssetGenerator( new UnityThreadComputeShaderExecutorObject()) }, //todo
            {MyShaderBufferType.PrincipalCurvature, new NPRPrincipalCurvatureAssetGenerator() },
            {MyShaderBufferType.TrimeshPrincipalCurvature, new NprTrimeshPrincipalCurvatureAssetGenerator() },
            {MyShaderBufferType.InterpolatedNormals, new NprInterpolatedNormalsAssetGenerator() },
        };

        public void Regenerate()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var mesh = GetComponent<MeshFilter>().mesh;

            var createdBuffers = new Dictionary<MyShaderBufferType, ComputeBuffer>();

            foreach (var order in Orders)
            {
                if (order.Enabled)
                {
                    var generator = _generatorsDict[order.BufferType];
                    var path = String.Format(_pathTemplate, order.BufferName, order.BufferType);

                    var requiredBuffers = generator.RequiredBuffers;
                    Preconditions.Assert(requiredBuffers.All(c => createdBuffers.ContainsKey(c)),
                        $"Not all required buffers {requiredBuffers.MyToString()} are in created buffers {createdBuffers.Keys}");

                    var newBuffer = generator.CreateAndSave(path, mesh, createdBuffers);

                    createdBuffers[order.BufferType] = newBuffer;
                }
            }
        }



        public void Start()
        {
            //Regenerate();
        }
    }

    [Serializable]
    public class ShaderBufferGenerationOrders
    {
        public bool Enabled;
        public String BufferName;
        public MyShaderBufferType BufferType;
    }
}
