namespace Assets.Heightmaps.submaps
{
    public class Submap
    {
        private HeightmapArray _heightmap;
        private SubmapPosition _submapPosition;
        private int _lodFactor;

        public Submap(HeightmapArray heightmap, SubmapPosition submapPosition, int lodFactor)
        {
            _heightmap = heightmap;
            _submapPosition = submapPosition;
            _lodFactor = lodFactor;
        }

        public HeightmapArray Heightmap
        {
            get { return _heightmap; }
        }

        public SubmapPosition SubmapPosition
        {
            get { return _submapPosition; }
        }

        public int LodFactor
        {
            get { return _lodFactor; }
        }
    }
}