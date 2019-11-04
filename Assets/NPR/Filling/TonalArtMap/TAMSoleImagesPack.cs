using System.Collections.Generic;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMSoleImagesPack
    {
        private readonly Dictionary<TAMTone, Dictionary<TAMMipmapLevel, Texture2D>> _columns;

        public TAMSoleImagesPack(Dictionary<TAMTone, Dictionary<TAMMipmapLevel, Texture2D>> columns)
        {
            _columns = columns;
        }

        public Dictionary<TAMTone, Dictionary<TAMMipmapLevel, Texture2D>> Columns => _columns;
    }
}