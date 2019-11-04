using Assets.Utils;

namespace Assets.ETerrain.SectorFilling
{
    /// <summary>
    /// //////////
    /// </summary>



    public interface ISegmentPlacer
    {
        void PlaceSegment(SegmentInformation segmentInfo);
        void ChangeSegmentState(SegmentInformation newSegmentInfo);
        void RemoveSegment(IntVector2 alignedSegmentPosition);
    }
}