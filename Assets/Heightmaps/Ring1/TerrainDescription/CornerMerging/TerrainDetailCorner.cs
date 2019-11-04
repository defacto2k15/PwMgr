using System.Collections.Generic;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging
{
    public class TerrainDetailCorner
    {
        private IntVector2 _movement;
        private string _name;

        private TerrainDetailCorner(IntVector2 movement, string name)
        {
            _movement = movement;
            _name = name;
        }

        public IntVector2 Movement => _movement;

        public static TerrainDetailCorner TopRight = new TerrainDetailCorner(new IntVector2(1, 1), "TopRight");
        public static TerrainDetailCorner BottomRight = new TerrainDetailCorner(new IntVector2(1, -1), "BottomRight");
        public static TerrainDetailCorner BottomLeft = new TerrainDetailCorner(new IntVector2(-1, -1), "BottomLeft");
        public static TerrainDetailCorner TopLeft = new TerrainDetailCorner(new IntVector2(-1, 1), "TopLeft");

        public static List<TerrainDetailCorner> AllDirections => OrderedDirections;

        public static List<TerrainDetailCorner> OrderedDirections = new List<TerrainDetailCorner>()
        {
            TopLeft,
            TopRight,
            BottomRight,
            BottomLeft,
        };

        public override string ToString()
        {
            return $"{nameof(_name)}: {_name}";
        }
    }
}