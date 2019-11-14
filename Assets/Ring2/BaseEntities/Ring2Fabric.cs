using Assets.Ring2.IntensityProvider;
using Assets.Utils;

namespace Assets.Ring2.BaseEntities
{
    public class Ring2Fabric
    {
        private Ring2Fiber _fiber;
        private Ring2FabricColors _paletteColors;
        private IFabricRing2IntensityProvider _intensityProvider;
        private float _layerPriority;
        private float _patternScale;

        public Ring2Fabric(Ring2Fiber fiber, Ring2FabricColors paletteColors,
            IFabricRing2IntensityProvider intensityProvider, float layerPriority, float patternScale)
        {
            _fiber = fiber;
            _paletteColors = paletteColors;
            _intensityProvider = intensityProvider;
            _layerPriority = layerPriority;
            _patternScale = patternScale;
            Preconditions.Assert(layerPriority<=1, "Layer priority should not be > than 0, but it is "+layerPriority);
        }

        public Ring2Fiber Fiber
        {
            get { return _fiber; }
        }

        public Ring2FabricColors PaletteColors
        {
            get { return _paletteColors; }
        }

        public IFabricRing2IntensityProvider IntensityProvider
        {
            get { return _intensityProvider; }
        }

        public float LayerPriority
        {
            get { return _layerPriority; }
        }

        public bool IsFirm
        {
            get { return _fiber.IsFirm; }
        }

        public float PatternScale => _patternScale;
    }
}