using System.Collections.Generic;
using Assets.Utils;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMDeckRendererConfiguration
    {
        public Dictionary<TAMMipmapLevel, IntVector2> SoleImagesResolutionPerLevel;
        public float Margin;
        public Dictionary<TAMMipmapLevel, float> StrokeHeightMultiplierPerLevel;
        public bool UseDithering;
        public bool UseSmoothAlpha;
    }

}