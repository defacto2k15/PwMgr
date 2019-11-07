using System.Collections.Generic;
using Assets.ETerrain.Pyramid.Shape;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class HeightPyramidLevelEntities
    {
        public List<PerGroundTypeEntities> PerGroundEntities;
        public HeightPyramidSegmentShapeGroup ShapeGroup;
        public HeightPyramidLocationUniformsGenerator LocationUniformsGenerator;
        public HeightPyramidGroupTransitionResolver TransitionResolver;
        public HeightPyramidLevelTemplate LevelTemplate;
        public HeightPyramidPerLevelConfiguration PerLevelConfiguration;
    }
}