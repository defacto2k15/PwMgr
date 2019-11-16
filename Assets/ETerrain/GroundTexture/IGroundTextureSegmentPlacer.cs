using System.Threading.Tasks;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public interface IGroundTextureSegmentPlacer
    {
        Task PlaceSegmentAsync(Texture segmentTexture, PlacementDetails placementDetails);
    }
}