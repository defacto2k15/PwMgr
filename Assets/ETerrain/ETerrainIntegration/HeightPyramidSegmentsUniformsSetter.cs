using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.Pyramid.Shape;
using Assets.Utils.ShaderBuffers;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class HeightPyramidSegmentsUniformsSetter
    {
        public void InitializePyramidUniforms(HeightPyramidSegmentShapeGroup @group, HeightPyramidLevel groupLevel,
            Dictionary<EGroundTextureType, RenderTexture> ceilTextureArrays, int levelsCount, int ringsPerLevelCount)
        {
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetInt("_LevelsCount", levelsCount);
                c.SetInt("_RingsPerLevelCount", ringsPerLevelCount);
                c.SetInt("_ThisLevelIndex", groupLevel.GetIndex());

                foreach (var pair in ceilTextureArrays)
                {
                    c.SetTexture("_"+ pair.Key.GetName(), pair.Value);
                }
            });
        }

        public void PassPyramidBuffers( HeightPyramidSegmentShapeGroup @group, ComputeBuffer configurationBuffer, BufferReloaderRootGO bufferReloaderRootGo, ComputeBuffer ePyramidPerFrameConfigurationBuffer)
        {
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetBuffer("_EPyramidConfigurationBuffer", configurationBuffer);
                bufferReloaderRootGo.RegisterBufferToReload(c, "_EPyramidConfigurationBuffer", configurationBuffer);

                c.SetBuffer("_EPyramidPerFrameConfigurationBuffer", ePyramidPerFrameConfigurationBuffer);
                bufferReloaderRootGo.RegisterBufferToReload(c, "_EPyramidPerFrameConfigurationBuffer", ePyramidPerFrameConfigurationBuffer);
            });
        }


        public void UpdateUniforms(HeightPyramidSegmentShapeGroup group, HeightPyramidLevel groupLevel, Vector2 travelerPosition
            , Dictionary<HeightPyramidLevel, LocationParametersUniforms> uniformsForAllLevels)
        {
            var thisLevelUniforms = uniformsForAllLevels[groupLevel];
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetVector("_TravellerPositionWorldSpace", travelerPosition); 
                c.SetVector("_MainPyramidCenterWorldSpace", thisLevelUniforms.PyramidCenterWorldSpace);
            });
        }
    }
}