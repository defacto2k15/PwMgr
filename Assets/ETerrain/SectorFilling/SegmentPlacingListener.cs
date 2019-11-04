using Assets.Utils;

namespace Assets.ETerrain.SectorFilling
{
    public interface SegmentPlacingListener
    {
        void ResponseSegmentWasAdded(IntVector2 alignedPosition);
    }
}