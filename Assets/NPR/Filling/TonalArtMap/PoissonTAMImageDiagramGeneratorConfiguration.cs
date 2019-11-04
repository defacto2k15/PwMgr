using System.Collections.Generic;

namespace Assets.NPRResources.TonalArtMap
{
    public class PoissonTAMImageDiagramGeneratorConfiguration
    {
        public int GenerationCount;
        public Dictionary<TAMTone, Dictionary<TAMMipmapLevel, float>> ExclusionZoneValues;
    }
}