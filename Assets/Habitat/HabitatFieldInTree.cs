using Assets.Utils.Quadtree;
using GeoAPI.Geometries;

namespace Assets.Habitat
{
    public class HabitatFieldInTree : IHasEnvelope, ICanTestIntersect
    {
        public HabitatField Field;

        public Envelope CalculateEnvelope()
        {
            return Field.Geometry.EnvelopeInternal;
        }

        public bool Intersects(IGeometry geometry)
        {
            return Field.Geometry.Intersects(geometry);
        }
    }
}