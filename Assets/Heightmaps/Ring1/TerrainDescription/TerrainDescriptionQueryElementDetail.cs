using System;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    [Serializable]
    public class TerrainDescriptionQueryElementDetail
    {
        public TerrainDescriptionElementTypeEnum Type;
        public TerrainCardinalResolution Resolution;
        public RequiredCornersMergeStatus RequiredMergeStatus = RequiredCornersMergeStatus.NOT_IMPORTANT;
    }
}