using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    [Serializable]
    public class TerrainDescriptionQuery
    {
        public MyRectangle QueryArea;
        public List<TerrainDescriptionQueryElementDetail> RequestedElementDetails;
    }
}