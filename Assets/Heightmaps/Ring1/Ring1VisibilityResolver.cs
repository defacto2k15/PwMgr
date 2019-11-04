using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1VisibilityResolver
    {
        private FovData _fovData = null;
        private bool? _visibilityOverride = null;
        private readonly Ring1BoundsCalculator _ring1BoundsCalculator;

        public Ring1VisibilityResolver(Ring1BoundsCalculator ring1BoundsCalculator,
            Vector3 initialCameraPosition = default(Vector3))
        {
            _ring1BoundsCalculator = ring1BoundsCalculator;
            _fovData = new FovData(initialCameraPosition, null);
        }

        public void SetFovData(FovData fovData)
        {
            _fovData = fovData;
        }

        public void SetFovDataOverride(bool? visibilityOverride)
        {
            _visibilityOverride = visibilityOverride;
        }

        public bool IsVisible(MyRectangle ring1NodePosition)
        {
            if (_visibilityOverride != null)
            {
                return _visibilityOverride.Value;
            }
            return _fovData.IsIn(_ring1BoundsCalculator.CalculateBounds(ring1NodePosition));
        }

        public Vector3 CameraPosition => _fovData.CameraPosition;
    }
}