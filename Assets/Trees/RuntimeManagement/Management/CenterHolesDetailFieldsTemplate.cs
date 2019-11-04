using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.TerrainMat;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries.Utilities;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.Management
{
    public class CenterHolesDetailFieldsTemplate : IDetailFieldsTemplate
    {
        private Dictionary<VegetationDetailLevel, IGeometry> _baseShapes =
            new Dictionary<VegetationDetailLevel, IGeometry>();

        public CenterHolesDetailFieldsTemplate(List<DetailFieldsTemplateOneLine> templateLines)
        {
            foreach (var pair in templateLines)
            {
                var level = pair.Level;
                var minLength = pair.MinRadius;
                var maxLength = pair.MaxRadius;

                IGeometry newPolygon = MyNetTopologySuiteUtils.CreateRectanglePolygon(maxLength);
                if (minLength > 0.001)
                {
                    newPolygon = MyNetTopologySuiteUtils.CreateRectanglePolygon(maxLength)
                        .Difference(MyNetTopologySuiteUtils.CreateRectanglePolygon(minLength));
                }
                _baseShapes[level] = newPolygon;
            }
        }

        public List<VegetationManagementArea> CalculateManagementAreas(Vector2 center, Vector2 positionDelta)
        {
            var centerTransformation = AffineTransformation.TranslationInstance(center.x, center.y);
            var minusDelta = center - positionDelta;
            var minusDeltaTransformation = AffineTransformation.TranslationInstance(minusDelta.x, minusDelta.y);

            Dictionary<VegetationDetailLevel, IGeometry> gainedAreas =
                new Dictionary<VegetationDetailLevel, IGeometry>();
            Dictionary<VegetationDetailLevel, IGeometry> lostAreas = new Dictionary<VegetationDetailLevel, IGeometry>();

            foreach (var level in VegetationDetailLevelUtils.AllFromSmallToBig())
            {
                var baseShape = _baseShapes[level];

                var newPolygon = MyNetTopologySuiteUtils.CloneWithTransformation(baseShape, centerTransformation);
                var oldPolygon = MyNetTopologySuiteUtils.CloneWithTransformation(baseShape, minusDeltaTransformation);

                var gainedArea = newPolygon.Difference(oldPolygon);
                var lostArea = oldPolygon.Difference(newPolygon);

                gainedAreas[level] = gainedArea;
                lostAreas[level] = lostArea;
            }

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
            foreach (var level in VegetationDetailLevelUtils.AllFromSmallToBig())
            {
                var baseShape = _baseShapes[level];
                var newShape = centerTransformation.Transform(baseShape.Clone() as IGeometry);
                gainedAreas[level] = newShape;
            }

            var allUsedLevels = gainedAreas.Keys;
            var areas =
                allUsedLevels.Select(c => new VegetationManagementArea(c, gainedAreas[c], null)).ToList();

            return (areas);
        }
    }
}