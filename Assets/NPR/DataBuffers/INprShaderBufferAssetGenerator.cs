using System.Collections.Generic;
using Assets.NPR.DataBuffers;
using UnityEngine;

namespace Assets.NPR.Lines
{
    public interface INprShaderBufferAssetGenerator
    {
        ComputeBuffer CreateAndSave(string path, Mesh mesh, Dictionary<MyShaderBufferType, ComputeBuffer> createdBuffers);
        List<MyShaderBufferType> RequiredBuffers { get; }
    }
}