using System.Collections.Generic;
using Assets.ETerrain.Pyramid.Map;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class ETerrainHeightBuffersManager
    {
        private EPyramidShaderBuffersGenerator _configurationBufferGenerator;

        private ComputeBuffer _ePyramidPerFrameParametersBuffer;
        private ComputeBuffer _ePyramidConfigurationBuffer;

        public ETerrainHeightBuffersManager()
        {
            _configurationBufferGenerator = new EPyramidShaderBuffersGenerator();
        }

        public void InitializeBuffers(
            Dictionary<HeightPyramidLevel, EPyramidShaderBuffersGeneratorPerRingInput> input, int maxLevelsCount, int maxRingsInLevelsCount)
        {
            _ePyramidConfigurationBuffer = _configurationBufferGenerator.GenerateConfigurationBuffer(input, maxLevelsCount,maxRingsInLevelsCount);
            _ePyramidPerFrameParametersBuffer = _configurationBufferGenerator.GenerateEPyramidPerFrameParametersBuffer( maxLevelsCount);
        }

        public ComputeBuffer EPyramidConfigurationBuffer => _ePyramidConfigurationBuffer;
        public ComputeBuffer PyramidPerFrameParametersBuffer => _ePyramidPerFrameParametersBuffer;

        public void UpdateEPyramidPerFrameParametersBuffer(List<HeightPyramidLevel> levels, int maxLevelsCount, Dictionary<HeightPyramidLevel, Vector2> pyramidCenterPerLevel)
        {
            _configurationBufferGenerator.UpdateEPyramidPerFrameParametersBuffer(_ePyramidPerFrameParametersBuffer, levels, maxLevelsCount, pyramidCenterPerLevel);
        }
    }
}