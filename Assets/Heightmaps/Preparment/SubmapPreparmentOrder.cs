using Assets.Heightmaps.submaps;

namespace Assets.Heightmaps.Preparment
{
    class SubmapPreparmentOrder
    {
        private SubmapPosition _position;
        private int _lodFactor;
        private readonly int _ringNumber;

        public SubmapPreparmentOrder(SubmapPosition position, int lodFactor, int ringNumber)
        {
            _position = position;
            _lodFactor = lodFactor;
            _ringNumber = ringNumber;
        }

        public SubmapPosition Position
        {
            get { return _position; }
        }

        public int LodFactor
        {
            get { return _lodFactor; }
        }

        public int RingNumber
        {
            get { return _ringNumber; }
        }
    }
}