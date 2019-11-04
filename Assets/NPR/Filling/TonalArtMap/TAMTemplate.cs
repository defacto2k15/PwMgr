using System.Collections.Generic;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMTemplate
    {
        private readonly Dictionary<TAMTone, Dictionary<TAMMipmapLevel, TAMImageDiagram>> _columns;

        public TAMTemplate(Dictionary<TAMTone, Dictionary<TAMMipmapLevel, TAMImageDiagram>> columns)
        {
            _columns = columns;
        }

        public Dictionary<TAMTone, Dictionary<TAMMipmapLevel, TAMImageDiagram>> Columns => _columns;
    }
}