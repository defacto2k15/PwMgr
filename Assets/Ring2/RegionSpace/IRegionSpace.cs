using GeoAPI.Geometries;

namespace Assets.Ring2.RegionSpace
{
    public interface IRegionSpace
    {
        Envelope Envelope { get; }

        IRegionSpaceDistanceProvider DistanceProvider { get; }
        bool IsEmpty { get; }
        bool Intersects(Envelope envelope);
    }
}