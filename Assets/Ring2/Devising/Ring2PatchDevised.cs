using System.Collections.Generic;
using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.Ring2.Devising
{
    public class Ring2PatchDevised
    {
        private MyRectangle _sliceArea;
        private List<Ring2Plate> _plates;

        public Ring2PatchDevised(List<Ring2Plate> plates, MyRectangle sliceArea)
        {
            _plates = plates;
            _sliceArea = sliceArea;
        }

        public List<Ring2Plate> Plates
        {
            get { return _plates; }
        }

        public MyRectangle SliceArea => _sliceArea;
    }
}