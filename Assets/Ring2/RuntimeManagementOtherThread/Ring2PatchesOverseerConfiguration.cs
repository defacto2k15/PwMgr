using System.Collections.Generic;
using UnityEngine;

namespace Assets.Ring2.RuntimeManagementOtherThread
{
    public class Ring2PatchesOverseerConfiguration
    {
        public Vector2 PatchSize { get; set; }
        public Dictionary<int, float> IntensityPatternPixelsPerUnit { get; set; }
    }
}