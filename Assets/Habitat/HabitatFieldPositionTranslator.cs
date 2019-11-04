using System.Linq;
using Assets.Roads.Pathfinding;
using Assets.TerrainMat;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace Assets.Habitat
{
    public class HabitatFieldPositionTranslator
    {
        private GeoCoordsToUnityTranslator _translator;

        public HabitatFieldPositionTranslator(GeoCoordsToUnityTranslator translator)
        {
            _translator = translator;
        }

        public HabitatField Translate(HabitatField input)
        {
            var polygon = input.Geometry as IPolygon;
            var movedPolygon = new Polygon(
                Translate(polygon.Shell),
                polygon.Holes.Select(c => Translate((ILinearRing) c)).ToArray()
            );
            return new HabitatField()
            {
                Geometry = movedPolygon,
                Type = input.Type
            };
        }

        private ILinearRing Translate(ILinearRing input)
        {
            return new LinearRing(
                input.Coordinates
                    .Select(c => MyNetTopologySuiteUtils.ToVector2(c))
                    .Select(c => _translator.TranslateToUnity(c))
                    .Select(c => MyNetTopologySuiteUtils.ToCoordinate(c))
                    .ToArray()
            );
        }
    }
}