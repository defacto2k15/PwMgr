using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Ring2;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using Assets.Utils.MT;
using Assets.Utils.Quadtree;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Trees.Placement.BiomesMap
{
    public class FromTextureVegetationBiomesMap : IVegetationBiomesMap
    {
        private readonly List<VegetationBiomeLevel> _biomeLevels;
        private readonly Texture2D _biomesTexture;
        private readonly PositionRemapper _remapper;

        public FromTextureVegetationBiomesMap(List<VegetationBiomeLevel> biomeLevels, Texture2D biomesTexture,
            PositionRemapper remapper)
        {
            _biomeLevels = biomeLevels;
            _biomesTexture = biomesTexture;
            _remapper = remapper;
        }

        public VegetationBiomeLevelComposition RetriveBiomesAt(MyRectangle queryRectangle)
        {
            var remappedPosition = _remapper.GetRemappedPosition(queryRectangle.Center);
            Vector4 biomeWeights = _biomesTexture.GetPixelBilinear(remappedPosition[0], remappedPosition[1]);
            biomeWeights = biomeWeights.normalized;

            var outLevels = _biomeLevels.Select((elem, index) => new BiomeLevelWithStrength(elem, biomeWeights[index]))
                .ToList();
            return new VegetationBiomeLevelComposition(outLevels);
        }
    }


    public class FromArrayVegetationBiomesMap : IVegetationBiomesMap
    {
        private readonly MySimpleArray<List<BiomeLevelWithStrength>> _biomesArray;
        private readonly MyRectangle _mapCoords;

        public FromArrayVegetationBiomesMap(MySimpleArray<List<BiomeLevelWithStrength>> biomesArray,
            MyRectangle mapCoords)
        {
            _biomesArray = biomesArray;
            _mapCoords = mapCoords;
        }

        public VegetationBiomeLevelComposition RetriveBiomesAt(MyRectangle queryRectangle)
        {
            var queryUv = RectangleUtils.CalculateSubelementUv(_mapCoords, queryRectangle.Center);
            var interpolationValues = MyArrayUtils.GetValueWithUvClamped(_biomesArray, queryUv);

            var strengthDictionary = new Dictionary<VegetationBiomeLevel, float>();
            foreach (var aInterpolationValue in interpolationValues)
            {
                var weight = aInterpolationValue.Weight;
                foreach (var aBiomeWithStrength in aInterpolationValue.Value)
                {
                    if (!strengthDictionary.ContainsKey(aBiomeWithStrength.BiomeLevel))
                    {
                        strengthDictionary.Add(aBiomeWithStrength.BiomeLevel, 0);
                    }
                    strengthDictionary[aBiomeWithStrength.BiomeLevel] += weight * aBiomeWithStrength.Strength;
                }
            }

            var normalized = DictionaryUtils.NormalizeValues(strengthDictionary);

            return new VegetationBiomeLevelComposition(
                normalized.Select(c => new BiomeLevelWithStrength(c.Key, c.Value)).ToList()
            );
        }
    }

    public class VegetationBiomesArrayGenerator
    {
        private readonly ITerrainHeightArrayProvider _terrainHeightArrayProvider;
        private readonly MyQuadtree<BiomeProviderAtArea> _biomeProvidersTree;
        private RandomFieldFigureGeneratorUTProxy _figureGenerator;
        private VegetationBiomesArrayGeneratorConfiguration _configuration;

        public VegetationBiomesArrayGenerator(
            ITerrainHeightArrayProvider terrainHeightArrayProvider,
            MyQuadtree<BiomeProviderAtArea> biomeProvidersTree, RandomFieldFigureGeneratorUTProxy figureGenerator,
            VegetationBiomesArrayGeneratorConfiguration configuration)
        {
            _terrainHeightArrayProvider = terrainHeightArrayProvider;
            _biomeProvidersTree = biomeProvidersTree;
            _figureGenerator = figureGenerator;
            _configuration = configuration;
        }

        public FromArrayVegetationBiomesMap Generate(
            TerrainCardinalResolution terrainResolution,
            IntVector2 arraySize,
            MyRectangle generationArea)
        {
            var msw = new MyStopWatch();

            msw.StartSegment("Random fieldFigureGenerating");
            var randomFieldFigure = _figureGenerator.GenerateRandomFieldFigureAsync(
                RandomFieldNature.FractalSimpleValueNoise3, 423, //todo seed!
                generationArea.ToRectangle()).Result; //todo take care of randomResolution!

            msw.StartSegment("HeightArrayCreation");
            var heightArrayWithUv =
                _terrainHeightArrayProvider.RetriveTerrainHeightInfo(generationArea, terrainResolution); //todo async?

            //Debug.Log("N66: "+arraySize);

            MySimpleArray<List<BiomeLevelWithStrength>> biomesArray =
                new MySimpleArray<List<BiomeLevelWithStrength>>(arraySize.X, arraySize.Y);

            msw.StartSegment("Rest");
            var oneCellSize = new Vector2(generationArea.Width / arraySize.X, generationArea.Height / arraySize.Y);
            for (int x = 0; x < arraySize.X; x++)
            {
                for (int y = 0; y < arraySize.Y; y++)
                {
                    var queryEnvelope = CalculateQueryEnvelope(generationArea, oneCellSize, x, y);
                    var foundBiomes = _biomeProvidersTree.Query(
                        queryEnvelope).Select(c => new
                    {
                        biomeProvider = c,
                        intersectionArea = c.IntersectionArea(queryEnvelope)
                    }).Where(c => c.intersectionArea > 0.0000001).ToList();
                    if (!foundBiomes.Any())
                    {
                        Debug.LogError("E77 there is biome array pixel withpout biomes. Should no be. Continuing");
                        biomesArray.SetValue(x, y, new List<BiomeLevelWithStrength>());
                        continue;
                    }

                    var maxPriority = foundBiomes.Max(c => c.biomeProvider.Priority);
                    var flooredPriority = Mathf.Floor(maxPriority);

                    var biomesWithCutOffPriority = foundBiomes
                        .Where(c => c.biomeProvider.Priority >= flooredPriority)
                        .Select(c => new
                        {
                            biomeProvider = c.biomeProvider,
                            intersectionArea = c.intersectionArea,
                            newPriority = c.biomeProvider.Priority - flooredPriority + 0.1
                        }).ToList();

                    var prioritiesSum = biomesWithCutOffPriority.Sum(c => c.newPriority);
                    var areaSum = biomesWithCutOffPriority.Sum(c => c.intersectionArea);
                    var biomeProvidersWithNormalizedPriorities =
                        biomesWithCutOffPriority.Select(c => new
                        {
                            intersectionArea = c.intersectionArea / areaSum,
                            c.biomeProvider,
                            newPriority = c.newPriority / prioritiesSum
                        }).ToList();


                    var randomValue =
                        randomFieldFigure.GetPixelWithUv(new Vector2(x / (float) arraySize.X, y / (float) arraySize.Y));
                    var randomHeightFactor = Mathf.Lerp(_configuration.RandomHeightJitterRange[0],
                        _configuration.RandomHeightJitterRange[1], randomValue);

                    var queryCenter = new MyRectangle(
                        generationArea.X + oneCellSize.x * x,
                        generationArea.Y + oneCellSize.y * y,
                        oneCellSize.x,
                        oneCellSize.y
                    ).Center;

                    var inHeightTextureUv = CalculateInHeightTextureUv(arraySize, x, y, heightArrayWithUv.UvBase);
                    var terrainInfo = new TerrainInfo()
                    {
                        Height = MyArrayUtils.GetValueWithUvClampedComputed(heightArrayWithUv.HeightArray,
                                     inHeightTextureUv) * randomHeightFactor,
                        FlatPosition = queryCenter
                    };


                    var biomes = biomeProvidersWithNormalizedPriorities.Select(c => new
                    {
                        retrivedBiomes = c.biomeProvider.BiomeProvider.RetriveBiomesAt(terrainInfo).Result, //todo
                        area = c.intersectionArea,
                        priority = c.newPriority
                    }).SelectMany(c =>
                    {
                        return c.retrivedBiomes.Select(
                            k => new BiomeLevelWithStrength(k.BiomeLevel,
                                (float) (k.Strength * c.area * c.priority)));
                    }).ToList();

                    var biomeLevelWithStrengths = RenormalizeBiomes(biomes);
                    biomesArray.SetValue(x, y, biomeLevelWithStrengths);
                }
            }
            //Debug.Log("b222" +msw.CollectResults());
            return new FromArrayVegetationBiomesMap(biomesArray, generationArea);
        }


        private static IGeometry CalculateQueryEnvelope(MyRectangle generationArea, Vector2 oneCellSize,
            int x,
            int y)
        {
            var queryRectangle = new MyRectangle(
                generationArea.X + oneCellSize.x * x,
                generationArea.Y + oneCellSize.y * y,
                oneCellSize.x,
                oneCellSize.y
            );

            var queryEnvelope = MyNetTopologySuiteUtils.ToGeometryEnvelope(queryRectangle);
            return queryEnvelope;
        }

        private static Vector2 CalculateInHeightTextureUv(IntVector2 arraySize, int x, int y,
            MyRectangle uvBase)
        {
            var inCellUv = new Vector2(x / (float) arraySize.X, y / (float) arraySize.Y);
            var inHeightTextureUv = RectangleUtils.CalculateSubPosition(uvBase, inCellUv);
            return inHeightTextureUv;
        }

        private static List<BiomeLevelWithStrength> RenormalizeBiomes(IEnumerable<BiomeLevelWithStrength> biomes)
        {
            var biomeAndStrengthDictionary = new Dictionary<VegetationBiomeLevel, float>();
            foreach (var aBiome in biomes)
            {
                if (!biomeAndStrengthDictionary.ContainsKey(aBiome.BiomeLevel))
                {
                    biomeAndStrengthDictionary[aBiome.BiomeLevel] = 0;
                }
                biomeAndStrengthDictionary[aBiome.BiomeLevel] += aBiome.Strength;
            }
            var normalizedBiomesDict = DictionaryUtils.NormalizeValues(biomeAndStrengthDictionary);
            var biomeLevelWithStrengths = normalizedBiomesDict.Select(
                c => new BiomeLevelWithStrength(c.Key, c.Value)).Where(c => c.Strength > 0).ToList();
            return biomeLevelWithStrengths;
        }

        public class VegetationBiomesArrayGeneratorConfiguration
        {
            public Vector2 RandomHeightJitterRange;
        }
    }

    public class BiomeProviderAtArea : IHasEnvelope, ICanTestIntersect, ICanTestIntersectionArea
    {
        private readonly IGeometry _areaGeometry;
        private readonly ITerrainInfoDependentBiomeProvider _biomeProvider;
        private readonly float _priority;

        public BiomeProviderAtArea(
            IGeometry areaGeometry,
            ITerrainInfoDependentBiomeProvider biomeProvider,
            float priority)
        {
            _areaGeometry = areaGeometry;
            _biomeProvider = biomeProvider;
            _priority = priority;
        }

        public Envelope CalculateEnvelope()
        {
            return _areaGeometry.EnvelopeInternal;
        }

        public bool Intersects(IGeometry geometry)
        {
            return _areaGeometry.Intersects(geometry);
        }

        public float IntersectionArea(IGeometry geometry)
        {
            try
            {
                return (float) _areaGeometry.Intersection(geometry).Area;
            }
            catch (Exception e)
            {
                Debug.LogError("EIntersection error, repair: " + e); //todo
                return 0;
            }
        }

        public ITerrainInfoDependentBiomeProvider BiomeProvider => _biomeProvider;

        public float Priority => _priority;
    }

    public interface ITerrainInfoDependentBiomeProvider
    {
        Task<List<BiomeLevelWithStrength>> RetriveBiomesAt(TerrainInfo info);
    }

    public class TerrainInfo
    {
        public float Height;
        public Vector2 FlatPosition;
    }

    public class HeightDependentBiomeProvider : ITerrainInfoDependentBiomeProvider
    {
        // works @ <-margin-heightRange, heightRange+margin>
        private readonly List<VegetationBiomeLevel> _biomes;

        private readonly MarginedRange _range;

        public HeightDependentBiomeProvider(List<VegetationBiomeLevel> biomes, MarginedRange range)
        {
            _biomes = biomes;
            _range = range;
        }

        public Task<List<BiomeLevelWithStrength>> RetriveBiomesAt(TerrainInfo info)
        {
            var height = info.Height;
            var strength = _range.PresenceFactor(height);
            return TaskUtils.MyFromResult(_biomes.Select(c => new BiomeLevelWithStrength(c, strength)).ToList());
        }
    }
}