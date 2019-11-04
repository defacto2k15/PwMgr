using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    // Not used now, now generation is by NPR Barycentric Coords Asset Generator Go
    public class BarycentricCoordinatesBufferSupplierOC : MonoBehaviour
    {
        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var mesh = GetComponent<MeshFilter>().mesh;
            var bufferGenerator = new BarycentricCoordinatesBufferGenerator( 
                "npr_barycentricBufferGenerator_comp",
                new UnityThreadComputeShaderExecutorObject() );

            var barycentricDataArray = bufferGenerator.Generate(mesh).Result;
            var buffer = new ComputeBuffer(mesh.vertices.Length, 2 * sizeof(float));
            buffer.SetData(barycentricDataArray);

            GetComponent<MeshRenderer>().material.SetBuffer("_BarycentricCoordinatesBuffer",buffer );
        }
    }
}
