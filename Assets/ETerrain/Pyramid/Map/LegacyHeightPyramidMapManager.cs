using System.Collections.Generic;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public class LegacyHeightPyramidMapManager
    {
        private HeightSegmentPlacer _placer;
        private Dictionary<HeightPyramidLevel, GroundLevelTexturesManager> _levelManagers;

        public LegacyHeightPyramidMapManager(HeightSegmentPlacer placer, LegacyHeightPyramidMapConfiguration configuration)
        {
            _placer = placer;
            _levelManagers =  new Dictionary<HeightPyramidLevel, GroundLevelTexturesManager>()
            {
                {HeightPyramidLevel.Bottom, new GroundLevelTexturesManager(configuration.SlotMapSize) },
                {HeightPyramidLevel.Mid, new GroundLevelTexturesManager(configuration.SlotMapSize) },
                {HeightPyramidLevel.Top, new GroundLevelTexturesManager(configuration.SlotMapSize) },
            };
        }

        public void AddSegment(Texture segmentTexture, HeightPyramidLevel levelOf, IntVector2 segmentAlignedPosition)
        {
            var placementDetails = _levelManagers[levelOf].Place(segmentAlignedPosition);
            _placer.PlaceSegment(segmentTexture, placementDetails);
        }
    }
}