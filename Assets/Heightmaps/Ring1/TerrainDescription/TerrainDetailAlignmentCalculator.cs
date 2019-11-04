using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainDetailAlignmentCalculator
    {
        private readonly int _terrainDetailImageSideDisjointResolution;

        public TerrainDetailAlignmentCalculator(int terrainDetailImageSideDisjointResolution)
        {
            _terrainDetailImageSideDisjointResolution = terrainDetailImageSideDisjointResolution;
        }

        public MyRectangle ComputeAlignedTerrainArea(MyRectangle queryArea,
            TerrainCardinalResolution cardinalResolution)
        {
            var newTerrainArea = TerrainShapeUtils.GetAlignedTerrainArea(queryArea, cardinalResolution,
                _terrainDetailImageSideDisjointResolution);
            return newTerrainArea;
        }
        
        public void AssertResolutionIsCompilant(MyRectangle queryArea,
            TerrainCardinalResolution elementDetailResolution)
        {
            //in other words, it is not too big
            var maxLength = elementDetailResolution.DetailResolution.MetersPerPixel *
                            _terrainDetailImageSideDisjointResolution;
            Preconditions.Assert(queryArea.Width <= maxLength, "Too wide query area. Width is " + queryArea.Width);
            Preconditions.Assert(queryArea.Height <= maxLength, "Too tall query area. Height is " + queryArea.Height);
        }

        public MyRectangle GetAlignedTerrainArea(MyRectangle queryArea, TerrainCardinalResolution cardinalResolution)
        {
            return TerrainShapeUtils.GetAlignedTerrainArea(queryArea, cardinalResolution,
                _terrainDetailImageSideDisjointResolution);
        }

        public IntVector2 GetGriddedTerrainArea(MyRectangle alignedArea,
            TerrainCardinalResolution cardinalResolution)
        {
            return TerrainShapeUtils.GetGriddedTerrainArea(alignedArea, cardinalResolution,
                _terrainDetailImageSideDisjointResolution);
        }

        public MyRectangle GetAlignedTerrainArea(IntVector2 griddedArea, TerrainCardinalResolution cardinalResolution)
        {
            return TerrainShapeUtils.GetAlignedTerrainArea(griddedArea, cardinalResolution,
                _terrainDetailImageSideDisjointResolution);
        }

        public bool AreaInMap(MyRectangle area)
        {
            //todo make configuration from it!!
            return area.X >= 0 && area.Y >= 0 && area.MaxX <= 5760 * 18 && area.MaxY <= 5760 * 18;
        }
    }
}