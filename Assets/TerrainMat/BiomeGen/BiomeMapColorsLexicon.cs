using System.Collections.Generic;

namespace Assets.TerrainMat.BiomeGen
{
    public class BiomeMapColorsLexicon
    {
        private List<ColorPack> _colorPacks;
        private Dictionary<BiomeInstanceId, int> _instanceIdToColorPackId;

        public BiomeMapColorsLexicon(List<ColorPack> colorPacks,
            Dictionary<BiomeInstanceId, int> instanceIdToColorPackId)
        {
            _colorPacks = colorPacks;
            _instanceIdToColorPackId = instanceIdToColorPackId;
        }

        public List<ColorPack> ColorPacks => _colorPacks;

        public int GetColorPackId(BiomeInstanceId id)
        {
            return _instanceIdToColorPackId[id];
        }
    }
}