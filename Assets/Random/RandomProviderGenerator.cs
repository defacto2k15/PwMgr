namespace Assets.Random
{
    public class RandomProviderGenerator
    {
        private int _seed;

        public RandomProviderGenerator(int seed)
        {
            _seed = seed;
        }

        public RandomProvider GetRandom()
        {
            return new RandomProvider(_seed);
        }
    }
}