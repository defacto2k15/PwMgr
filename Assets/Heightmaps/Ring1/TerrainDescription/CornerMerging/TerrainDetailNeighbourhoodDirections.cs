using System.Collections.Generic;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging
{
    public class TerrainDetailNeighbourhoodDirections
    {
        private IntVector2 _movement;
        private string _name;

        private TerrainDetailNeighbourhoodDirections(IntVector2 movement, string name)
        {
            _movement = movement;
            _name = name;
        }

        public IntVector2 Movement => _movement;

        public static TerrainDetailNeighbourhoodDirections Top = new TerrainDetailNeighbourhoodDirections(new IntVector2(0, 1), "Top");
        public static TerrainDetailNeighbourhoodDirections Right = new TerrainDetailNeighbourhoodDirections(new IntVector2(1, 0),"Right");
        public static TerrainDetailNeighbourhoodDirections Bottom = new TerrainDetailNeighbourhoodDirections(new IntVector2(0, -1),"Bottom");
        public static TerrainDetailNeighbourhoodDirections Left = new TerrainDetailNeighbourhoodDirections(new IntVector2(-1, 0),"Left");
        public static TerrainDetailNeighbourhoodDirections TopRight = new TerrainDetailNeighbourhoodDirections(new IntVector2(1, 1), "TopRight");
        public static TerrainDetailNeighbourhoodDirections BottomRight = new TerrainDetailNeighbourhoodDirections(new IntVector2(1, -1), "BottomRight");
        public static TerrainDetailNeighbourhoodDirections BottomLeft = new TerrainDetailNeighbourhoodDirections(new IntVector2(-1, -1), "BottomLeft");
        public static TerrainDetailNeighbourhoodDirections TopLeft = new TerrainDetailNeighbourhoodDirections(new IntVector2(-1, 1), "TopLeft");

        public static List<TerrainDetailNeighbourhoodDirections> AllDirections = new List<TerrainDetailNeighbourhoodDirections>()
        {
            Top,
            Right,
            Bottom,
            Left,
            TopRight,
            BottomRight,
            BottomLeft,
            TopLeft
        };

        public override string ToString()
        {
            return $"{nameof(_name)}: {_name}";
        }
    }
}