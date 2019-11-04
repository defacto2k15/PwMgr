using System.Collections.Generic;
using Assets.Utils;

namespace Assets.ETerrain.Pyramid.Map
{
    public class SegmentNeighbourhoodCorner
    {
        private IntVector2 _movement;
        private string _name;

        private SegmentNeighbourhoodCorner(IntVector2 movement, string name)
        {
            _movement = movement;
            _name = name;
        }

        public IntVector2 Movement => _movement;

        public static SegmentNeighbourhoodCorner TopRight = new SegmentNeighbourhoodCorner(new IntVector2(1, 1), "TopRight");
        public static SegmentNeighbourhoodCorner BottomRight = new SegmentNeighbourhoodCorner(new IntVector2(1, -1), "BottomRight");
        public static SegmentNeighbourhoodCorner BottomLeft = new SegmentNeighbourhoodCorner(new IntVector2(-1, -1), "BottomLeft");
        public static SegmentNeighbourhoodCorner TopLeft = new SegmentNeighbourhoodCorner(new IntVector2(-1, 1), "TopLeft");

        public static List<SegmentNeighbourhoodCorner> AlLCorners = new List<SegmentNeighbourhoodCorner>()
        {
            TopRight,
            BottomRight,
            BottomLeft,
            TopLeft
        };

        public SegmentNeighbourhoodCorner Opposite 
        {
            get
            {
                if (this == TopRight)
                {
                    return BottomLeft;
                }
                if (this == TopLeft)
                {
                    return BottomRight;
                }
                if (this == BottomLeft)
                {
                    return TopRight;
                }
                if (this == BottomRight)
                {
                    return TopLeft;
                }
                Preconditions.Fail("Cannot find opposite of corner "+this);
                return null;
            }
        }

        public override string ToString()
        {
            return $"{nameof(_name)}: {_name}";
        }
    }
}