using System;

namespace Assets.ETerrain.SectorFilling
{
    public class LambdaSegmentFillingListener : ISegmentFillingListener
    {
        private Action<SegmentInformation> _addSegment;
        private Action<SegmentInformation> _removeSegment;
        private Action<SegmentInformation> _changeSegmentState;

        public LambdaSegmentFillingListener(Action<SegmentInformation> addSegment, Action<SegmentInformation> removeSegment, Action<SegmentInformation> changeSegmentState)
        {
            _addSegment = addSegment;
            _removeSegment = removeSegment;
            _changeSegmentState = changeSegmentState;
        }

        public void AddSegment(SegmentInformation segmentInfo)
        {
            _addSegment(segmentInfo);
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
            _removeSegment(segmentInfo);
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
            _changeSegmentState(segmentInfo);
        }
    }
}