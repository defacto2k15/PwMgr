using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class RankedTerrainFeatureApplier
    {
        public int Rank;
        public ITerrainFeatureApplier Applier;
        public List<TerrainCardinalResolution> AvalibleResolutions;
    }
}