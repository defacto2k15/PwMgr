using Assets.Utils;

namespace Assets.ETerrain.Pyramid.Map
{
    public static class SegmentNeighbourDiregmentUtils
    {
        public static SegmentNeighbourhoodDiregment Opposite(this SegmentNeighbourhoodDiregment diregment)
        {
            if (diregment == SegmentNeighbourhoodDiregment.Bottom)
            {
                return SegmentNeighbourhoodDiregment.Top;
            }else if (diregment == SegmentNeighbourhoodDiregment.Left)
            {
                return SegmentNeighbourhoodDiregment.Right;
            }else if (diregment == SegmentNeighbourhoodDiregment.Right)
            {
                return SegmentNeighbourhoodDiregment.Left;
            }else if (diregment == SegmentNeighbourhoodDiregment.Top)
            {
                return SegmentNeighbourhoodDiregment.Bottom;
            }
            else
            {
                Preconditions.Fail($"Not supported diregment {diregment}");
                return SegmentNeighbourhoodDiregment.Bottom;
            }
        }
    }
}