namespace Assets.Heightmaps
{
    public class ValueRange
    {
        private readonly float _min;
        private readonly float _max;

        public ValueRange(float min, float max)
        {
            _min = min;
            _max = max;
        }

        public float Min
        {
            get { return _min; }
        }

        public float Max
        {
            get { return _max; }
        }
    }
}