using System.Collections.Generic;

namespace Assets.ETerrain.SectorFilling
{
    public class SegmentFieldDelta
    {
        public List<SegmentInformation> SectorsToCreate;
        public List<SegmentInformation> SectorsToRemove;
        public List<SegmentInformation> SectorsToChange;
    }
}