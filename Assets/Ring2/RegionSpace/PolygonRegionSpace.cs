using Assets.TerrainMat;
using GeoAPI.Geometries;

namespace Assets.Ring2.RegionSpace
{
    public class PolygonRegionSpace : IRegionSpace
    {
        private IPolygon _polygon;

        public PolygonRegionSpace(IPolygon polygon)
        {
            _polygon = polygon;
        }

        public Envelope Envelope
        {
            get { return _polygon.EnvelopeInternal; }
        }

        public IRegionSpaceDistanceProvider DistanceProvider
        {
            get { return new PolygonRegionSpaceDistanceProvider(_polygon); }
        }

        public bool IsEmpty
        {
            get { return _polygon.IsEmpty; }
        }

        public bool Intersects(Envelope sliceArea)
        {
            return _polygon.Intersects(MyNetTopologySuiteUtils.ToGeometryEnvelope(sliceArea));
        }

        protected bool Equals(PolygonRegionSpace other)
        {
            return Equals(_polygon, other._polygon);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PolygonRegionSpace) obj);
        }

        public override int GetHashCode()
        {
            return (_polygon != null ? _polygon.GetHashCode() : 0);
        }
    }
}