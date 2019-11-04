using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.Heightmaps.submaps
{
    public class SubmapPosition : Positions2D<int>
    {
        public SubmapPosition(int downLeftX, int width, int downLeftY, int height)
            : base(downLeftX, downLeftY, width, height)
        {
        }

        public Point2D DownLeftPoint
        {
            get { return new Point2D(DownLeftX, DownLeftY); }
        }

        public Point2D DownRightPoint
        {
            get { return new Point2D(DownLeftX + Width, DownLeftY); }
        }

        public Point2D TopLeftPoint
        {
            get { return new Point2D(DownLeftX, DownLeftY + Height); }
        }

        public Point2D TopRightPoint
        {
            get { return new Point2D(DownLeftX + Width, DownLeftY + Height); }
        }

        public int GetHeightPoint(int i, int currentHeight)
        {
            return DownLeftY + Height * i / currentHeight;
        }

        public int GetWidthPoint(int i, int currentWidth)
        {
            return DownLeftX + Width * i / currentWidth;
        }

        public bool IsApexPoint(Point2D apexPoint)
        {
            return Equals(TopLeftPoint, apexPoint) || Equals(TopRightPoint, apexPoint) ||
                   Equals(DownLeftPoint, apexPoint) ||
                   Equals(DownRightPoint, apexPoint);
        }

        public bool IsPointPartOfSubmap(Point2D point)
        {
            return point.X >= DownLeftPoint.X && point.X <= DownRightPoint.X && point.Y >= DownLeftPoint.Y &&
                   point.Y <= TopLeftPoint.Y;
        }

        public override string ToString()
        {
            return string.Format("SubmapPosition: DownLeftX: {0}, DownLeftY: {1}, Width: {2}, Height: {3}", DownLeftX,
                DownLeftY, Width, Height);
        }
    }
}