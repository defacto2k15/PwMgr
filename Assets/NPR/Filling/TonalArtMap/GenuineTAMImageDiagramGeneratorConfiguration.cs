using System.Collections.Generic;

namespace Assets.NPRResources.TonalArtMap
{
    public class GenuineTAMImageDiagramGeneratorConfiguration
    {
        public Dictionary<TAMTone, float> TargetCoverages { get; set; }
        public Dictionary<TAMMipmapLevel, int> TriesCount { get; set; }
    }
}