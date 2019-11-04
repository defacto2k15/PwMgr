using System.Collections.Generic;
using System.Linq;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.NPR.DataBuffers;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.NPR.Lines
{
    public class NprBarycentricCoordsAssetGeneratorGo : MonoBehaviour
    {
        public string DestinationPath = "Assets/NPRResources/Curvature/Human.barycentric.asset";

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var mesh = GetComponent<MeshFilter>().mesh;
            var generator = new NprBarycentricCoordsAssetGenerator();
            generator.CreateAndSave(DestinationPath, mesh, new Dictionary<MyShaderBufferType, ComputeBuffer>());
        }
    }

    public class NprBarycentricCoordsAssetGenerator : INprShaderBufferAssetGenerator
    {
        public ComputeBuffer CreateAndSave(string path, Mesh mesh, Dictionary<MyShaderBufferType, ComputeBuffer> createdBuffers)
        {
            var bufferGenerator = new BarycentricCoordinatesBufferGenerator(
                "npr_barycentricBufferGenerator_comp",
                new UnityThreadComputeShaderExecutorObject());

            var barycentricDataArray = bufferGenerator.Generate(mesh).Result;

            var buffer = ScriptableObject.CreateInstance<ShaderBufferSE>();
            buffer.Data = barycentricDataArray;
            MyAssetDatabase.CreateAndSaveAsset(buffer, path);

            var computeBuffer = new ComputeBuffer(barycentricDataArray.Length/2, sizeof(float) * 2);
            computeBuffer.SetData(buffer.Data);
            return computeBuffer;
        }

        public List<MyShaderBufferType> RequiredBuffers => new List<MyShaderBufferType>();
    }
}