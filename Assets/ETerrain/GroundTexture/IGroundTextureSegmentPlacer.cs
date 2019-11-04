using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public interface IGroundTextureSegmentPlacer
    {
        void PlaceSegment(Texture segmentTexture, PlacementDetails placementDetails);
    }
}