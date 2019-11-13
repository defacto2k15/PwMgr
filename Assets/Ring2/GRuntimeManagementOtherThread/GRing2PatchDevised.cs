using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;

namespace Assets.Ring2.GRuntimeManagementOtherThread
{
    public class GRing2PatchDevised
    {
        private MyRectangle _sliceArea;
        private List<UniformsWithKeywords> _sliceInfos;
        private readonly Action _destructionAction;

        public GRing2PatchDevised(MyRectangle sliceArea, List<UniformsWithKeywords> sliceInfos, Action destructionAction)
        {
            _sliceArea = sliceArea;
            _sliceInfos = sliceInfos;
            _destructionAction = destructionAction;
        }

        public MyRectangle SliceArea => _sliceArea;

        public List<UniformsWithKeywords> SliceInfos => _sliceInfos;

        public void Destroy()
        {
            _destructionAction();
        }
    }
}