using System.Collections.Generic;
using System.Linq;
using Assets.Utils;

namespace Assets.ETerrain.SectorFilling
{
    public class SegmentPlacingSupervisior : ISegmentFillingListener, SegmentPlacingListener
    {
        private ISegmentPlacer _placer;
        private Dictionary<IntVector2, SegmentState> _segmentsWaitingToBeCreated;

        public SegmentPlacingSupervisior(ISegmentPlacer placer, Dictionary<IntVector2, SegmentState> segmentsWaitingToBeCreated)
        {
            _placer = placer;
            _segmentsWaitingToBeCreated = segmentsWaitingToBeCreated;
        }

        public void AddSegment(SegmentInformation segmentInfo)
        {
            _placer.PlaceSegment(segmentInfo);
            _segmentsWaitingToBeCreated[segmentInfo.SegmentAlignedPosition] = segmentInfo.SegmentState;
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
            var alignedPosition = segmentInfo.SegmentAlignedPosition;
            _placer.RemoveSegment(alignedPosition);
            if (_segmentsWaitingToBeCreated.ContainsKey(alignedPosition))
            {
                _segmentsWaitingToBeCreated.Remove(alignedPosition);
            }
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
            if (_segmentsWaitingToBeCreated.ContainsKey(segmentInfo.SegmentAlignedPosition))
            {
                _segmentsWaitingToBeCreated[segmentInfo.SegmentAlignedPosition] = segmentInfo.SegmentState;
                _placer.ChangeSegmentState(segmentInfo);
            }
        }

        public void ResponseSegmentWasAdded(IntVector2 position)
        {
            if (_segmentsWaitingToBeCreated.ContainsKey(position))
            {
                _segmentsWaitingToBeCreated.Remove(position);
            }
        }

        public bool AnyActiveSegmentsBeingLoaded()
        {
            return _segmentsWaitingToBeCreated.All(c => c.Value != SegmentState.Active); 
        }
    }
}