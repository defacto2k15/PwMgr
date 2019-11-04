using System.Collections.Generic;
using Assets.Utils;

namespace Assets.ETerrain.Pyramid.Map
{
    public class PlacementDetails
    {
        public IntVector2 ModuledPositionInGrid;
        public List<SegmentCornerToModify> CornersToModify;
    }
}