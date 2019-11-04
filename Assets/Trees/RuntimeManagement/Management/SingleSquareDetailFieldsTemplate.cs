using System.Collections.Generic;
using System.Linq;
using Assets.TerrainMat;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries.Utilities;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.Management
{
    public class SingleSquareDetailFieldsTemplate : IDetailFieldsTemplate
    {
        private IGeometry _baseShape;
        private VegetationDetailLevel _onlyLevel;

        public SingleSquareDetailFieldsTemplate(float sideLength, VegetationDetailLevel onlyLevel)
        {
            _onlyLevel = onlyLevel;
            _baseShape = MyNetTopologySuiteUtils.CreateRectanglePolygon(sideLength / 2);
        }

        public List<VegetationManagementArea> CalculateManagementAreas(Vector2 center, Vector2 positionDelta)
        {
            var centerTransformation = AffineTransformation.TranslationInstance(center.x, center.y);
            var minusDelta = center - positionDelta;
            var minusDeltaTransformation = AffineTransformation.TranslationInstance(minusDelta.x, minusDelta.y);

            Dictionary<VegetationDetailLevel, IGeometry> gainedAreas =
                new Dictionary<VegetationDetailLevel, IGeometry>();
            Dictionary<VegetationDetailLevel, IGeometry> lostAreas = new Dictionary<VegetationDetailLevel, IGeometry>();

            var baseShape = _baseShape;

            var newPolygon = MyNetTopologySuiteUtils.CloneWithTransformation(baseShape, centerTransformation);
            var oldPolygon = MyNetTopologySuiteUtils.CloneWithTransformation(baseShape, minusDeltaTransformation);

            var gainedArea = newPolygon.Difference(oldPolygon);
            var lostArea = oldPolygon.Difference(newPolygon);

            gainedAreas[_onlyLevel] = gainedArea;
            lostAreas[_onlyLevel] = lostArea;

            var allUsedLevels = gainedAreas.Keys;
            var areas =
                allUsedLevels.Select(c => new VegetationManagementArea(c, gainedAreas[c], lostAreas[c])).ToList();

            return (areas);
        }

        public List<VegetationManagementArea> CalculateInitialManagementAreas(Vector2 center)
        {
            var centerTransformation = AffineTransformation.TranslationInstance(center.x, center.y);

            Dictionary<VegetationDetailLevel, IGeometry> gainedAreas =
                new Dictionary<VegetationDetailLevel, IGeometry>();
            var baseShape = _baseShape;
            var newShape = centerTransformation.Transform(baseShape.Clone() as IGeometry);
            gainedAreas[_onlyLevel] = newShape;

            var allUsedLevels = gainedAreas.Keys;
            var areas =
                allUsedLevels.Select(c => new VegetationManagementArea(c, gainedAreas[c], null)).ToList();

            return (areas);
        }
    }
}