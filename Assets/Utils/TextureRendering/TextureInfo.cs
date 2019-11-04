namespace Assets.Utils.TextureRendering
{
    public class TextureInfo
    {
        private readonly int _x;
        private readonly int _y;
        private int width;
        private int height;

        public TextureInfo(int x, int y, int width, int height)
        {
            _x = x;
            _y = y;
            this.width = width;
            this.height = height;
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public int X
        {
            get { return _x; }
        }

        public int Y
        {
            get { return _y; }
        }
    }
}