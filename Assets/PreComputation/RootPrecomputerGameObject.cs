using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using UnityEngine;

namespace Assets.PreComputation
{
    public class RootPrecomputerGameObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            var rootPrecomputer = new RootPrecomputer();
            rootPrecomputer.Compute(ComputeShaderContainer);
            //rootPrecomputer.FillTerrainDetailsCache(ComputeShaderContainer);
        }
    }
}