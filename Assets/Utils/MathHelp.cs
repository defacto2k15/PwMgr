namespace Assets.Utils
{
    class MathHelp
    {
        public static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        public static bool SegmentsHaveCommonElement(int i1, int i2, int j1, int j2)
        {
            return (i1 <= j1 && i2 > j1) || (j1 <= i1 && j2 > i1);
        }
    }
}