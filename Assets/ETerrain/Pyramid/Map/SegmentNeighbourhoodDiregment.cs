using System.Collections.Generic;
using Assets.Utils;

namespace Assets.ETerrain.Pyramid.Map
{
    public class SegmentNeighbourhoodDiregment
    {
        private IntVector2 _movement;
        private string _name;

        private SegmentNeighbourhoodDiregment(IntVector2 movement, string name)
        {
            _movement = movement;
            _name = name;
        }

        public IntVector2 Movement => _movement;

        public static SegmentNeighbourhoodDiregment Top = new SegmentNeighbourhoodDiregment(new IntVector2(0, 1), "Top");
        public static SegmentNeighbourhoodDiregment Right = new SegmentNeighbourhoodDiregment(new IntVector2(1, 0),"Right");
        public static SegmentNeighbourhoodDiregment Bottom = new SegmentNeighbourhoodDiregment(new IntVector2(0, -1),"Bottom");
        public static SegmentNeighbourhoodDiregment Left = new SegmentNeighbourhoodDiregment(new IntVector2(-1, 0),"Left");
        public static SegmentNeighbourhoodDiregment TopRight = new SegmentNeighbourhoodDiregment(new IntVector2(1, 1), "TopRight");
        public static SegmentNeighbourhoodDiregment BottomRight = new SegmentNeighbourhoodDiregment(new IntVector2(1, -1), "BottomRight");
        public static SegmentNeighbourhoodDiregment BottomLeft = new SegmentNeighbourhoodDiregment(new IntVector2(-1, -1), "BottomLeft");
        public static SegmentNeighbourhoodDiregment TopLeft = new SegmentNeighbourhoodDiregment(new IntVector2(-1, 1), "TopLeft");

        public static List<SegmentNeighbourhoodDiregment> AllDiregments = new List<SegmentNeighbourhoodDiregment>()
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