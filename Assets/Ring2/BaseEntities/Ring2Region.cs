using System.Collections;
using System.Linq;
using Assets.Ring2.RegionSpace;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace Assets.Ring2.BaseEntities
{
    public class Ring2Region
    {
        private IRegionSpace _space;
        private Ring2Substance _substance;
        private float _magnitude;

        public Ring2Region(IRegionSpace space, Ring2Substance substance, float magnitude)
        {
            _space = space;
            _substance = substance;
            _magnitude = magnitude;
        }

        public Envelope RegionEnvelope
        {
            get { return _space.Envelope; }
        }

        public IRegionSpace Space
        {
            get { return _space; }
        }

        public Ring2Substance Substance
        {
            get { return _substance; }
        }

        public float Magnitude
        {
            get { return _magnitude; }
        }
    }
}