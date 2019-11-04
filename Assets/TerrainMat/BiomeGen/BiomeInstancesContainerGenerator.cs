using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using UnityEngine;

namespace Assets.TerrainMat.BiomeGen
{
    public class BiomeInstancesContainerGenerator
    {
        private BiomesContainerGeneratorConfiguration _configuration;
        private uint _lastBiomeId = 0;

        public BiomeInstancesContainerGenerator(BiomesContainerGeneratorConfiguration configuration)
        {
            _configuration = configuration;
        }

        public BiomeInstancesContainer Generate(
            BiomesContainerConfiguration configuration,
            List<BiomeInstancePlacementTemplate> templates,
            BiomeInstancesContainer initialContainer = null
        )
        {
            BiomeInstancesContainer container = null;
            if (initialContainer != null)
            {
                container = initialContainer;
            }
            else
            {
                container = new BiomeInstancesContainer(configuration);
            }
            for (float x = 0; x < 1; x += _configuration.GenerationSpaceSize.x)
            {
                for (float y = 0; y < 1; y += _configuration.GenerationSpaceSize.y)
                {
                    var generationSpace = new MyRectangle(x, y, _configuration.GenerationSpaceSize.x,
                        _configuration.GenerationSpaceSize.y);

                    List<BiomeInstanceInfo> generatedBiomes = templates
                        .SelectMany((t, i) => GenerateBiomeInfos(unchecked((int) (i * 521 + x * 122 + y)), t,
                            generationSpace)).ToList();
                    foreach (var biome in generatedBiomes)
                    {
                        container.AddBiome(biome);
                    }
                }
            }

            return container;
        }

        private List<BiomeInstanceInfo> GenerateBiomeInfos(int randomSeed,
            BiomeInstancePlacementTemplate instancePlacementTemplate, MyRectangle generationSpace)
        {
            var area = generationSpace.Area;
            var random = new RandomProvider(randomSeed);
            int biomesToGenerateCount =
                Mathf.RoundToInt((float) (area *
                                          Mathf.Max(0,
                                              (float) random.RandomGaussian(instancePlacementTemplate
                                                  .OccurencesPerSquareUnit))));
            //Debug.Log($"T1X: {randomSeed}, toGen: {biomesToGenerateCount}");

            List<BiomeInstanceInfo> toReturn = new List<BiomeInstanceInfo>();
            toReturn = Enumerable.Range(0, biomesToGenerateCount).Select(i =>
            {
                var perInsanceRandom = new RandomProvider(randomSeed + (i + 1) * 43);
                var centerPoint =
                    new Vector2(generationSpace.X, generationSpace.Y) +
                    new Vector2(
                        perInsanceRandom.Next(0, generationSpace.Width),
                        perInsanceRandom.Next(0, generationSpace.Height));

                var points = instancePlacementTemplate.LeafPointsGenerator.GenerateLeafPoints(
                        444 + (i + 1) * (randomSeed + 1),
                        instancePlacementTemplate.LeafPointsCount,
                        instancePlacementTemplate.LeafPointsDistanceCharacteristics)
                    .Select(c => centerPoint + c).ToList();

                var convexHull = new MultiPoint(
                    points.Select(c => MyNetTopologySuiteUtils.ToPoint(c)).Cast<IPoint>().ToArray()
                ).ConvexHull() as IPolygon;

                return new PolygonBiomeInstanceInfo(
                    instancePlacementTemplate.Type,
                    convexHull,
                    new BiomeInstanceId(_lastBiomeId++), 0);
            }).Cast<BiomeInstanceInfo>().Reverse().ToList();
            return toReturn;
        }
    }

    public class BiomesContainerGeneratorConfiguration
    {
        public Vector2 GenerationSpaceSize = new Vector2(1, 1);
    }
}