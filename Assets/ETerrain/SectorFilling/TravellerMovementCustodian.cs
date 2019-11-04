namespace Assets.ETerrain.SectorFilling
{
    public class TravellerMovementCustodian
    {
        private SegmentPlacingSupervisior _placingSupervisior;

        public TravellerMovementCustodian(SegmentPlacingSupervisior placingSupervisior)
        {
            _placingSupervisior = placingSupervisior;
        }

        public bool CanMove()
        {
            return _placingSupervisior.AnyActiveSegmentsBeingLoaded();
        }
    }
}