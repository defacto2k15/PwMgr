using Assets.ETerrain.ETerrainIntegration.deos;
using Assets.ETerrain.SectorFilling;

namespace Assets.ETerrain.Sector
{
    public class UnityThreadDummySegmentFillingListener : ISegmentFillingListener
    {
        private OtherThreadCompoundSegmentFillingOrdersExecutorProxy _executor;

        public UnityThreadDummySegmentFillingListener(OtherThreadCompoundSegmentFillingOrdersExecutorProxy executor)
        {
            _executor = executor;
        }

        public void AddSegment(SegmentInformation segmentInfo)
        {
            var token = new SegmentGenerationProcessToken(SegmentGenerationProcessSituation.BeforeStartOfCreation, RequiredSegmentSituation.Filled);
            _executor.ExecuteSegmentAction(token,segmentInfo.SegmentAlignedPosition);
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
        }
    }
}