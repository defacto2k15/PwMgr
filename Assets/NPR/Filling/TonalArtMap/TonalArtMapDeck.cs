using System.Collections.Generic;
using System.Drawing;

namespace Assets.NPRResources.TonalArtMap
{
    public class TonalArtMapDeck
    {
        private readonly Dictionary<TAMTone, Dictionary<TAMMipmapLevel, Image>> _columns;

        public TonalArtMapDeck(Dictionary<TAMTone, Dictionary<TAMMipmapLevel, Image>> columns)
        {
            _columns = columns;
        }

        public Dictionary<TAMTone, Dictionary<TAMMipmapLevel, Image>> Columns => _columns;
    }
}