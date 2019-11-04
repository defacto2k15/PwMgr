using System.Collections.Generic;
using GeoAPI.Geometries;

namespace Assets.Ring2.PatchTemplateToPatch
{
    public class Ring2Patch
    {
        private readonly Envelope _sliceArea;
        private readonly List<Ring2Slice> _slices;

        public Ring2Patch(Envelope sliceArea, List<Ring2Slice> slices)
        {
            _sliceArea = sliceArea;
            _slices = slices;
        }

        public Envelope SliceArea
        {
            get { return _sliceArea; }
        }

        public List<Ring2Slice> Slices
        {
            get { return _slices; }
        }
    }
}