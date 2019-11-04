using GeoAPI.Geometries;

namespace Assets.Trees.RuntimeManagement.Management
{
    public class VegetationManagementArea
    {
        private readonly VegetationDetailLevel _level;
        private readonly IGeometry _gainedArea;
        private readonly IGeometry _lostArea;

        public VegetationManagementArea(VegetationDetailLevel level, IGeometry gainedArea, IGeometry lostArea)
        {
            _level = level;
            _gainedArea = gainedArea;
            _lostArea = lostArea;
        }

        public VegetationDetailLevel Level
        {
            get { return _level; }
        }

        public IGeometry LostArea
        {
            get { return _lostArea; }
        }

        public IGeometry GainedArea
        {
            get { return _gainedArea; }
        }
    }
}