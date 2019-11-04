using GeoAPI.Geometries;

namespace Assets.Utils.Quadtree
{
    public interface ICanTestIntersectionArea
    {
        float IntersectionArea(IGeometry geometry);
    }
}