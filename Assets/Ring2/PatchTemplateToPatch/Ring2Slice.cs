using Assets.Ring2.BaseEntities;
using Assets.Utils;
using UnityEngine;

namespace Assets.Ring2.PatchTemplateToPatch
{
    public class Ring2Slice
    {
        private Ring2PatchSliceIntensityPattern _intensityPattern;
        private readonly ShaderKeywordSet _keywords;
        private readonly Ring2SlicePalette _slicePalette;
        private readonly Vector4 _layerPriorities;
        private Vector4 _randomSeeds;

        public Ring2Slice(ShaderKeywordSet keywords, Ring2SlicePalette slicePalette, Vector4 layerPriorities, Vector4 randomSeeds)
        {
            _keywords = keywords;
            _slicePalette = slicePalette;
            _layerPriorities = layerPriorities;
            _randomSeeds = randomSeeds;
        }

        public Ring2PatchSliceIntensityPattern IntensityPattern
        {
            get
            {
                Preconditions.Assert(_intensityPattern != null, "Intensity pattern is not set");
                return _intensityPattern;
            }
            set => _intensityPattern = value;
        }

        public ShaderKeywordSet Keywords => _keywords;

        public Ring2SlicePalette SlicePalette => _slicePalette;

        public Vector4 LayerPriorities => _layerPriorities;

        public Vector4 RandomSeeds => _randomSeeds;
    }
}