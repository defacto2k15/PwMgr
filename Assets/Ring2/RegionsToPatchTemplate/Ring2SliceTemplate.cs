using Assets.Ring2.BaseEntities;
using GeoAPI.Geometries;

namespace Assets.Ring2.RegionsToPatchTemplate
{
    public class Ring2SliceTemplate
    {
        private readonly Ring2Substance _substance;
        private readonly IGeometry _area;

        public Ring2SliceTemplate(Ring2Substance substance /*, IGeometry area*/)
        {
            _substance = substance;
//            _area = area;
        }

        public Ring2Substance Substance
        {
            get { return _substance; }
        }
    }
}