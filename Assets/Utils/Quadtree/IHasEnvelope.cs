using GeoAPI.Geometries;

namespace Assets.Utils.Quadtree
{
    public interface IHasEnvelope
    {
        Envelope CalculateEnvelope();
    }
}