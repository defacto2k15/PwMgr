using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.Pyramid.Map;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class EPyramidShaderBuffersGenerator
    {
        struct ERingConfiguration
        {
            public Vector2 UvRange;
            public Vector2 MergeRange;
        };

        struct ELevelConfiguration
        {
            public ERingConfiguration[] RingsConfiguration;
            public float CeilTextureWorldSize;
            public int CeilTextureResolution;
        };

        struct EPyramidConfiguration
        {
            public ELevelConfiguration[] LevelsConfiguration;
        };

        public ComputeBuffer GenerateConfigurationBuffer(Dictionary<HeightPyramidLevel,  EPyramidShaderBuffersGeneratorPerRingInput> input, int maxLevelsCount, int maxRingsInLevelsCount)
        {
            var ePyramidConfiguration = GenerateConfiguration(input);

            var floatsInBufferCount = maxLevelsCount * (maxRingsInLevelsCount * 4 + 2);

            var floatsArray = Enumerable.Range(0, floatsInBufferCount).Select(c => 0f).ToArray();

            var index = 0;
            foreach(var levelConfiguration in ePyramidConfiguration.LevelsConfiguration)
            {
                foreach (var ringConfiguration in levelConfiguration.RingsConfiguration)
                {
                    floatsArray[index + 0] = ringConfiguration.UvRange.x;
                    floatsArray[index + 1] = ringConfiguration.UvRange.y;
                    floatsArray[index + 2] = ringConfiguration.MergeRange.x;
                    floatsArray[index + 3] = ringConfiguration.MergeRange.y;
                    index += 4;
                }

                floatsArray[index] = levelConfiguration.CeilTextureWorldSize;
                floatsArray[index + 1] = levelConfiguration.CeilTextureResolution;
                index += 2;
            }


            var buffer = new ComputeBuffer(1,floatsArray.Length* sizeof(float), ComputeBufferType.Default);
            buffer.SetData(floatsArray);
            return buffer;
        }

        public ComputeBuffer GenerateEPyramidPerFrameParametersBuffer( int maxLevelsCount)
        {
            var floatsArray = new float[maxLevelsCount*2];
            var buffer = new ComputeBuffer(1,floatsArray.Length* sizeof(float), ComputeBufferType.Default);
            return buffer;
        }

        public void UpdateEPyramidPerFrameParametersBuffer(ComputeBuffer buffer, List<HeightPyramidLevel> levels, int maxLevelsCount, Dictionary<HeightPyramidLevel, Vector2> pyramidCenterPerLevel)
        {
            var floatsArray = Enumerable.Range(0, maxLevelsCount * 2).Select(c => 0f).ToArray();
            for (var i = 0; i < levels.Count; i++)
            {
                var heightPyramidLevel = levels[i];
                floatsArray[i*2+0] = pyramidCenterPerLevel[heightPyramidLevel].x;
                floatsArray[i*2+1] = pyramidCenterPerLevel[heightPyramidLevel].y;
            }
            buffer.SetData(floatsArray);
        }

        private EPyramidConfiguration GenerateConfiguration(Dictionary<HeightPyramidLevel,  EPyramidShaderBuffersGeneratorPerRingInput> input)
        {
            var levels = input.Keys.OrderBy(c => c.GetIndex());
            var worldSizes = input.Values.Select(c => c.CeilSliceWorldSize).ToList();
            var ceilTextureResolution = input.Values.Select(c => c.CeilTextureResolution).ToList();

            return new EPyramidConfiguration()
            {
                LevelsConfiguration = levels.Select(i =>
                {
                    return new ELevelConfiguration
                    {
                        RingsConfiguration = Enumerable.Range(0, input[i].RingUvRanges.Count)
                            .Select(c => new ERingConfiguration()
                            {
                                UvRange = input[i].RingUvRanges[c],
                                MergeRange = input[i].HeightMergeRanges[c]
                            }).ToArray(),
                        CeilTextureWorldSize = input[i].CeilSliceWorldSize,
                        CeilTextureResolution = input[i].CeilTextureResolution
                    };
                }).ToArray()
            };
        }
    }
}