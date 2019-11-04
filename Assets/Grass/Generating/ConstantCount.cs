namespace Assets.Grass
{
    internal class ConstantCount : IEntitiesCountProvider
    {
        private readonly int _count;

        public ConstantCount(int count)
        {
            _count = count;
        }

        public int GetCount()
        {
            return _count;
        }
    }
}