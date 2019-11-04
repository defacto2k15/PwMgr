using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public class SoleLevelGroundTextureSegmentModificationsManager
    {
        private IGroundTextureSegmentPlacer _placer;
        private GroundLevelTexturesManager _levelTexturesManager;

        public SoleLevelGroundTextureSegmentModificationsManager(IGroundTextureSegmentPlacer placer, GroundLevelTexturesManager levelTexturesManager)
        {
            _placer = placer;
            _levelTexturesManager = levelTexturesManager;
        }

        public void AddSegment(Texture segmentTexture, IntVector2 segmentAlignedPosition)
        {
            var placementDetails = _levelTexturesManager.Place(segmentAlignedPosition);
            _placer.PlaceSegment(segmentTexture, placementDetails);
        }
    }
}