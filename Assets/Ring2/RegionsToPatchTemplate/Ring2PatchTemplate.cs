using System.Collections.Generic;
using GeoAPI.Geometries;

namespace Assets.Ring2.RegionsToPatchTemplate
{
    public class Ring2PatchTemplate
    {
        private Envelope _sliceArea;
        private List<Ring2SliceTemplate> _sliceTemplates;

        public Ring2PatchTemplate(List<Ring2SliceTemplate> sliceTemplates, Envelope sliceArea)
        {
            _sliceTemplates = sliceTemplates;
            _sliceArea = sliceArea;
        }

        public Envelope SliceArea
        {
            get { return _sliceArea; }
        }

        public List<Ring2SliceTemplate> SliceTemplates
        {
            get { return _sliceTemplates; }
        }
    }
}