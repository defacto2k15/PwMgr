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
        public void InitializePerRingUniforms( HeightPyramidSegmentShapeGroup @group, HeightPyramidLevel groupLevel,
            Dictionary<HeightPyramidLevel, List<EGroundTexture>> levelsTexture, Dictionary<HeightPyramidLevel, float> pyramidLevelsWorldSizes )
        {
            if (group.CentralShape != null)
            {
                levelsTexture[groupLevel].ForEach(t => SetHeightmapUniformsToShape(group.CentralShape, t, null));
            }

            var maxRingIndex = group.ShapesPerRing.Keys.Max();
            group.ShapesPerRing[maxRingIndex]
                .ForEach(c => levelsTexture[groupLevel].ForEach(t => SetHeightmapUniformsToShape(c, t,
                    GetLowerLevelTexture(groupLevel, levelsTexture.ToDictionary(r => r.Key, r => r.Value.First(k => k.TextureType == t.TextureType))))));

            group.ShapesPerRing.Where(c => c.Key != maxRingIndex).SelectMany(c => c.Value).ToList()
                .ForEach(c => levelsTexture[groupLevel].ForEach(t =>  SetHeightmapUniformsToShape(c, t, null)));


            var higherLevel = groupLevel.GetHigherLevel();
            if (higherLevel.HasValue && levelsTexture.ContainsKey(higherLevel.Value))
            {
                group.CentralShapeMaterial.SetInt("_HigherLevelAreaCutting", 1);
                group.CentralShapeMaterial.SetFloat("_AuxPyramidLevelWorldSize", pyramidLevelsWorldSizes[higherLevel.Value]);
            }
            
            var lowerLevel = groupLevel.GetLowerLevel();
            if (lowerLevel.HasValue && levelsTexture.ContainsKey(lowerLevel.Value))
            {
                group.ShapesPerRing.SelectMany(c => c.Value).ToList().ForEach(
                    c => c.GetComponent<MeshRenderer>().material.SetFloat("_AuxPyramidLevelWorldSize", pyramidLevelsWorldSizes[lowerLevel.Value]));
            }
        }

        public void InitializePyramidUniforms( HeightPyramidSegmentShapeGroup @group, HeightPyramidLevel groupLevel,
            Dictionary<HeightPyramidLevel, float> pyramidLevelsWorldSizes, HeightPyramidCommonConfiguration heightPyramidMapConfiguration,
            Dictionary<HeightPyramidLevel, List<EGroundTexture>> levelTextures, int levelsCount, int ringsPerLevelCount)
        {
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetInt("_LevelsCount", levelsCount);
                c.SetInt("_RingsPerLevelCount", ringsPerLevelCount);
                c.SetFloat("_MainPyramidLevelWorldSize", pyramidLevelsWorldSizes[groupLevel]);
                c.SetVector("_LastRingSegmentUvRange", heightPyramidMapConfiguration.RingsUvRange[2/* TODO*/]);

                foreach (var pair in levelTextures)
                {
                    foreach (var groundTexture in pair.Value)
                    {
                        c.SetTexture("_" + groundTexture.Name + pair.Key.GetIndex(), groundTexture.Texture);
                    }
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


        public void UpdateUniforms(HeightPyramidSegmentShapeGroup group, HeightPyramidLevel groupLevel, Vector2 travelerPosition, Dictionary<HeightPyramidLevel, LocationParametersUniforms> uniformsForAllLevels)
        {
            var thisLevelUniforms = uniformsForAllLevels[groupLevel];
            group.ETerrainMaterials.ForEach(c =>
            {
                c.SetVector("_TravellerPositionWorldSpace", travelerPosition); 
                c.SetVector("_MainPyramidCenterWorldSpace", thisLevelUniforms.PyramidCenterWorldSpace);
            });

            if (groupLevel == HeightPyramidLevel.Top) // TODO
            { 
                //Shader.SetGlobalVector("_GlobalTravellerPosition", thisLevelUniforms.TravellerPosition);
            }

            var higherLevel = groupLevel.GetHigherLevel();
            if (higherLevel.HasValue && uniformsForAllLevels.ContainsKey(higherLevel.Value))
            {
                var higherLevelUniforms = uniformsForAllLevels[higherLevel.Value];
                var mat = group.CentralShapeMaterial;
                mat.SetVector("_AuxPyramidCenterWorldSpace", higherLevelUniforms.PyramidCenterWorldSpace);
            }

            var lowerLevel = groupLevel.GetLowerLevel();
            if (lowerLevel.HasValue && uniformsForAllLevels.ContainsKey(lowerLevel.Value))
            {
                var lowerLevelUniforms = uniformsForAllLevels[lowerLevel.Value];
                group.ShapesPerRing.SelectMany(c => c.Value).ToList().ForEach(
                    c => c.GetComponent<MeshRenderer>().material.SetVector("_AuxPyramidCenterWorldSpace", lowerLevelUniforms.PyramidCenterWorldSpace));
            }
        }

        private void SetHeightmapUniformsToShape(GameObject groupCentralShape, EGroundTexture mainTexture, EGroundTexture auxTexture)
        {
            var material = groupCentralShape.GetComponent<MeshRenderer>().material;
            material.SetTexture("_Main" + mainTexture.Name, mainTexture.Texture);
            var auxTexturePresent = 0;
            if (auxTexture != null)
            {
                auxTexturePresent = 1;
                material.SetTexture("_Aux"+auxTexture.Name, auxTexture.Texture);
            }
            material.SetInt("_Aux"+mainTexture.Name+"Mode", auxTexturePresent);
        }

        private Texture GetHigherLevelTexture(HeightPyramidLevel level, Dictionary<HeightPyramidLevel, Texture> levelsTexture)
        {
            var higher = level.GetHigherLevel();
            if (higher.HasValue)
            {
                if (levelsTexture.ContainsKey(higher.Value))
                {
                    return levelsTexture[higher.Value];
                }
            }

            return null;
        }

        private EGroundTexture GetLowerLevelTexture(HeightPyramidLevel level, Dictionary<HeightPyramidLevel, EGroundTexture> levelsTexture)
        {
            var lower = level.GetLowerLevel();
            if (lower.HasValue)
            {
                if (levelsTexture.ContainsKey(lower.Value))
                {
                    return levelsTexture[lower.Value];
                }
            }

            return null;
        }
    }
}