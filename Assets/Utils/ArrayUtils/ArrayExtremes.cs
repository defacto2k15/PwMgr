namespace Assets.Utils.ArrayUtils
{
    public class ArrayExtremes
    {
        public float Min;
        public float Max;

        public ArrayExtremes(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Delta => Max - Min;
    }
}