namespace Assets.Heightmaps
{
    public class GlobalHeightmapInfo
    {
        private int heightMaxValue;
        private int heightMinValue;
        private int realLifeWidth;
        private int realLifeHeight;

        public GlobalHeightmapInfo(int heightMaxValue, int heightMinValue, int realLifeWidth, int realLifeHeight)
        {
            this.heightMaxValue = heightMaxValue;
            this.heightMinValue = heightMinValue;
            this.realLifeWidth = realLifeWidth;
            this.realLifeHeight = realLifeHeight;
        }

        public int HeightMaxValue
        {
            get { return heightMaxValue; }
        }

        public int HeightMinValue
        {
            get { return heightMinValue; }
        }

        public int RealLifeWidth
        {
            get { return realLifeWidth; }
        }

        public int RealLifeHeight
        {
            get { return realLifeHeight; }
        }

        public float HeightDelta
        {
            get { return heightMaxValue - heightMinValue; }
        }
    }
}