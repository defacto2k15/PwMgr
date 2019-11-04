using System.Collections.Generic;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;

namespace Assets.Heightmaps.GRing
{
    public class GRingGroundShapeDetail
    {
        public UniformsPack Uniforms;
        public ShaderKeywordSet ShaderKeywordSet = new ShaderKeywordSet(new List<string>());
        public IGroundShapeToken GroundShapeToken;
        public TerrainDetailElementOutput HeightDetailOutput;
    }
}