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

        public Ring2Slice(ShaderKeywordSet keywords, Ring2SlicePalette slicePalette, Vector4 layerPriorities)
        {
            _keywords = keywords;
            _slicePalette = slicePalette;
            _layerPriorities = layerPriorities;
        }

        public Ring2PatchSliceIntensityPattern IntensityPattern
        {
            get
            {
                Preconditions.Assert(_intensityPattern != null, "Intensity pattern is not set");
                return _intensityPattern;
            }
            set { _intensityPattern = value; }
        }

        public ShaderKeywordSet Keywords
        {
            get { return _keywords; }
        }

        public Ring2SlicePalette SlicePalette
        {
            get { return _slicePalette; }
        }

        public Vector4 LayerPriorities
        {
            get { return _layerPriorities; }
        }
    }
}