namespace Assets.ETerrain.SectorFilling
{
    public interface ISegmentFillingListener
    {
        void AddSegment(SegmentInformation segmentInfo);
        void RemoveSegment(SegmentInformation segmentInfo);
        void SegmentStateChange(SegmentInformation segmentInfo);
    }
}