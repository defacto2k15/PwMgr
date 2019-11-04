using System.Collections.Generic;
using System.Linq;
using Assets.TerrainMat;
using GeoAPI.Geometries;

namespace Assets.Ring2.RegionSpace
{
    public class MultipolygonRegionSpace : IRegionSpace
    {
        private List<IPolygon> _polygons;

        public MultipolygonRegionSpace(List<IPolygon> polygons)
        {
            _polygons = polygons;
        }

        public Envelope Envelope
        {
            get
            {
                var allEnvelopes = _polygons.Select(c => c.EnvelopeInternal).ToList();
                double minX = allEnvelopes.Min(c => c.MinX);
                double maxX = allEnvelopes.Max(c => c.MaxX);
                double minY = allEnvelopes.Min(c => c.MinY);
                double maxY = allEnvelopes.Max(c => c.MaxY);
                return new Envelope(
                    minX,
                    maxX,
                    minY,
                    maxY);
            }
        }

        public IRegionSpaceDistanceProvider DistanceProvider
        {
            get
            {
                return
                    new AggregateRegionSpaceDistanceProvider(
                        _polygons.Select(c => new PolygonRegionSpaceDistanceProvider(c)).ToList());
            }
        }

        public bool IsEmpty
        {
            get { return _polygons.All(c => c.IsEmpty); }
        }

        public bool Intersects(Envelope sliceArea)
        {
            return _polygons.Any(c => c.Intersects(MyNetTopologySuiteUtils.ToGeometryEnvelope(sliceArea)));
        }

        protected bool Equals(MultipolygonRegionSpace other)
        {
            return Equals(_polygons, other._polygons);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MultipolygonRegionSpace) obj);
        }

        public override int GetHashCode()
        {
            return (_polygons != null ? _polygons.GetHashCode() : 0);
        }
    }
}