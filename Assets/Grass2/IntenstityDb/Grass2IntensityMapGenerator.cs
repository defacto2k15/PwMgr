using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Grass2.Planting;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads;
using Assets.Utils;
using Assets.Utils.Spatial;
using GeoAPI.Operation.Buffer;
using NetTopologySuite.Operation.Buffer;
using UnityEngine;

namespace Assets.Grass2.IntenstityDb
{
    public class Grass2IntensityMapGenerator : IStoredPartsGenerator<List<Grass2TypeWithIntensity>>
    {
        private HabitatToGrassIntensityMapGenerator _habitatToGrassIntensityMapGenerator;
        private HabitatTexturesGenerator _habitatTexturesGenerator;
        private Grass2IntensityMapGeneratorConfiguration _configuration;
        private PathProximityTextureDbProxy _pathProximityTextureDb;

        public Grass2IntensityMapGenerator(
            HabitatToGrassIntensityMapGenerator habitatToGrassIntensityMapGenerator,
            HabitatTexturesGenerator habitatTexturesGenerator,
            Grass2IntensityMapGeneratorConfiguration configuration, PathProximityTextureDbProxy pathProximityTextureDb)
        {
            _habitatToGrassIntensityMapGenerator = habitatToGrassIntensityMapGenerator;
            _habitatTexturesGenerator = habitatTexturesGenerator;
            _configuration = configuration;
            _pathProximityTextureDb = pathProximityTextureDb;
        }

        public async Task<List<Grass2TypeWithIntensity>> GenerateMapsAsync(MyRectangle queryArea)
        {
            var habitatTextureSize = new IntVector2(
                Mathf.CeilToInt(queryArea.Width / _configuration.HabitatSamplingUnit),
                Mathf.CeilToInt(queryArea.Height / _configuration.HabitatSamplingUnit)
            );
            var habitatTexturesDict =
                await _habitatTexturesGenerator.GenerateHabitatTextures(queryArea, habitatTextureSize);

            if (!habitatTexturesDict.Any())
            {
                return new List<Grass2TypeWithIntensity>();
            }

            UvdSizedTexture pathProximityTexture = await _pathProximityTextureDb.Query(queryArea);
            var toReturn = await _habitatToGrassIntensityMapGenerator.GenerateGrassIntenstiyAsync(queryArea,
                habitatTexturesDict, habitatTextureSize, pathProximityTexture);
            return toReturn;
        }

        public async Task<CoordedPart<List<Grass2TypeWithIntensity>>> GeneratePartAsync(
            MyRectangle queryArea)
        {
            var list = await GenerateMapsAsync(queryArea);
            return new CoordedPart<List<Grass2TypeWithIntensity>>()
            {
                Part = list,
                Coords = queryArea
            };
        }

        public class Grass2IntensityMapGeneratorConfiguration
        {
            public float HabitatSamplingUnit;
        }
    }
}