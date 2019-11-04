namespace Assets.Heightmaps.Ring1.valTypes
{
    public class UvPosition : MyRectangle
    {
        public UvPosition(float x, float y, float width, float height) : base(x, y, width, height)
        {
        }

        public new UvPosition DownLeftSubElement()
        {
            return new UvPosition(X, Y, Width / 2, Height / 2);
        }

        public new UvPosition DownRightSubElement()
        {
            return new UvPosition(X + Width / 2, Y, Width / 2, Height / 2);
        }

        public new UvPosition TopRightSubElement()
        {
            return new UvPosition(X, Y + Height / 2, Width / 2, Height / 2);
        }

        public new UvPosition TopLeftSubElement()
        {
            return new UvPosition(X + Width / 2, Y + Height / 2, Width / 2, Height / 2);
        }
    }
}