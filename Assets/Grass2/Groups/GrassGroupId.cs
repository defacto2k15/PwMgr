namespace Assets.Grass2.Groups
{
    public struct GrassGroupId
    {
        public int Id;

        public bool Equals(GrassGroupId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GrassGroupId && Equals((GrassGroupId) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        private static int _lastId = 0;

        public static GrassGroupId GenerateNext => new GrassGroupId()
        {
            Id = _lastId++
        };

        public static GrassGroupId Empty => new GrassGroupId()
        {
            Id = -1
        };

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}";
        }
    }
}