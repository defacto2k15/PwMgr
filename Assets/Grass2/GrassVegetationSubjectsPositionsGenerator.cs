using System.Collections.Generic;
using System.Linq;
using Assets.TerrainMat;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.RuntimeManagement;
using Assets.Utils;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Grass2
{
    public class GrassVegetationSubjectsPositionsGenerator : IVegetationSubjectsPositionsProvider
    {
        private GrassVegetationSubjectsPositionsGeneratorConfiguration _configuration;

        public GrassVegetationSubjectsPositionsGenerator(GrassVegetationSubjectsPositionsGeneratorConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<VegetationSubjectEntity> GetEntiesFrom(IGeometry area, VegetationDetailLevel level)
        {
            var outPositions = new List<Vector2>();

            if (level == VegetationDetailLevel.FULL)
            {
                var envelope = area.EnvelopeInternal;
                var gridPositionsStart = new IntVector2(
                    Mathf.CeilToInt((float) envelope.MinX / _configuration.PositionsGridSize.x),
                    Mathf.CeilToInt((float) envelope.MinY / _configuration.PositionsGridSize.y));

                var afterGridingLength = new Vector2(
                    (float) (envelope.MaxX - gridPositionsStart.X * _configuration.PositionsGridSize.x),
                    (float) (envelope.MaxY - gridPositionsStart.Y * _configuration.PositionsGridSize.y));

                var gridLength = new IntVector2( //ceil becouse there is point at length 0 !!
                    Mathf.CeilToInt(afterGridingLength.x / _configuration.PositionsGridSize.x),
                    Mathf.CeilToInt(afterGridingLength.y / _configuration.PositionsGridSize.y));

                for (int x = 0; x < gridLength.X; x++)
                {
                    for (int y = 0; y < gridLength.Y; y++)
                    {
                        var position = new Vector2(
                            (0.5f + (gridPositionsStart.X + x)) * _configuration.PositionsGridSize.x,
                            (0.5f + (gridPositionsStart.Y + y)) * _configuration.PositionsGridSize.y);

                        outPositions.Add(position);
                    }
                }
            }

            return outPositions.Where(c => area.Contains(MyNetTopologySuiteUtils.ToGeometryEnvelope(
                MyNetTopologySuiteUtils.ToPointEnvelope(c)))).Select(c => new VegetationSubjectEntity(
                new DesignBodyLevel0Detail()
                {
                    Pos2D = c,
                    Radius = 0,
                    Size = 0,
                    SpeciesEnum = VegetationSpeciesEnum.Grass2SpotMarker
                })).ToList();
        }

        public class GrassVegetationSubjectsPositionsGeneratorConfiguration
        {
            public Vector2 PositionsGridSize = new Vector2(10, 10);
        }
    }
}