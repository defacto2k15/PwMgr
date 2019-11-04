using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using Assets.TerrainMat.BiomeGen;
using Assets.Utils;
using UnityEngine;

namespace Assets.TerrainMat.Stain
{
    public class StainTerrainArrayFromBiomesGenerator : ITerrainArrayGenerator
    {
        private readonly BiomeInstancesContainer _biomeInstancesContainer;
        private readonly RandomProvider _randomProvider = new RandomProvider(555); //todo!
        private readonly BiomeInstanceDetailGenerator _detailGenerator;
        private readonly StainSpaceToUnitySpaceTranslator _spaceTranslator;
        private readonly StainTerrainArrayFromBiomesGeneratorConfiguration _configuration;
        private readonly Ring1CoordinatesSkewer _coordinatesSkewer;

        public StainTerrainArrayFromBiomesGenerator(BiomeInstancesContainer biomeInstancesContainer,
            BiomeInstanceDetailGenerator detailGenerator,
            StainSpaceToUnitySpaceTranslator spaceTranslator,
            StainTerrainArrayFromBiomesGeneratorConfiguration configuration)
        {
            _biomeInstancesContainer = biomeInstancesContainer;
            _configuration = configuration;
            _detailGenerator = detailGenerator;
            _spaceTranslator = spaceTranslator;
            _coordinatesSkewer = new Ring1CoordinatesSkewer(_configuration.NoSkewingDistance);
        }

        public StainTerrainArray ProvideData()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("TerrainPaletteArray");
            var characteristicsArray =
                GenerateInstanceCharacteristicsArray(_configuration.TexturesSideLength, _biomeInstancesContainer);
            msw.StartSegment("After char array");
            var arraySize = new IntVector2(characteristicsArray.GetLength(0), characteristicsArray.GetLength(1));

            var characteristicsSet = new HashSet<BiomeInstanceCharacteristics>();
            for (int x = 0; x < arraySize.X; x++)
            {
                for (int y = 0; y < arraySize.Y; y++)
                {
                    var charac = characteristicsArray[x, y];
                    characteristicsSet.Add(charac);
                }
            }

            var colorsLexicon = _detailGenerator.GenerateColorsLexicon(characteristicsSet.ToList());

            var terrainPaletteArray = colorsLexicon.ColorPacks;

            var paletteIndexArray = new int[arraySize.X, arraySize.Y];
            for (int x = 0; x < arraySize.X; x++)
            {
                for (int y = 0; y < arraySize.Y; y++)
                {
                    var instanceId = characteristicsArray[x, y].InstanceId;
                    paletteIndexArray[x, y] = colorsLexicon.GetColorPackId(instanceId);
                }
            }


            var idsSet = new HashSet<BiomeInstanceId>();
            var controlArray = new Vector4[arraySize.X, arraySize.Y];
            for (int x = 0; x < arraySize.X; x++)
            {
                for (int y = 0; y < arraySize.Y; y++)
                {
                    var characteristics = characteristicsArray[x, y];
                    controlArray[x, y] =
                        _detailGenerator.GenerateControlValues(x, y, characteristics.Type, characteristics.InstanceId);
                    idsSet.Add(characteristics.InstanceId);
                }
            }

            Debug.Log("T65 after generation: " + msw.CollectResults());
            return new StainTerrainArray(terrainPaletteArray, controlArray, paletteIndexArray);
        }


        private BiomeInstanceCharacteristics[,] GenerateInstanceCharacteristicsArray
            (int texturesSideLength, BiomeInstancesContainer biomeInstancesContainer)
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Precomputing data");
            var width = texturesSideLength;
            var height = texturesSideLength;


            List<MyPolygon> queryPolygons = new List<MyPolygon>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var queryPolygon = new MyPolygon(new[]
                        {
                            (new Vector2((float) x / width, (float) y / height)),
                            (new Vector2((float) (x + 1) / width, (float) y / height)),
                            (new Vector2((float) (x + 1) / width, (float) (y + 1) / height)),
                            (new Vector2((float) x / width, (float) (y + 1) / height)),
                        }.Select(c => _coordinatesSkewer.SkewPoint(c)).Select(c => _spaceTranslator.Translate(c))
                        .ToArray());
                    queryPolygons.Add(queryPolygon);
                }
            }
            msw.StartSegment("Data computation");
            var biomes = biomeInstancesContainer.BulkGetBiomeTypesIn(queryPolygons);
            msw.StartSegment("After computation");

            var outArray = new BiomeInstanceCharacteristics[texturesSideLength, texturesSideLength];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var biomesInGrid = biomes[y * width + x].Select(n => new
                    {
                        weight = n.Area * n.Characteristics.Priority * _configuration.PriorityWeightFactor,
                        biome = n
                    });

                    //var maxPriority = biomesInGrid.Max(c => c.Characteristics.Priority);
                    //var elementsOfGoodPriority = biomesInGrid.Where(c => c.Characteristics.Priority == maxPriority).ToList();

                    outArray[x, y] = biomesInGrid.OrderByDescending(c => c.weight).First().biome.Characteristics;
                }
            }

            //var arrDict = new Dictionary<BiomeType, int>();
            //for (var x = 0; x < width; x++)
            //{
            //    for (var y = 0; y < height; y++)
            //    {
            //        var type = outArray[x,y].Type;
            //        if (!arrDict.ContainsKey(type))
            //        {
            //            arrDict[type] = 0;
            //        }
            //        arrDict[type]++;
            //    }
            //}
            Debug.Log("T63 " + msw.CollectResults());

            return outArray;
        }
    }

    public class Ring1CoordinatesSkewer
    {
        private float _noSkewingDistance;

        public Ring1CoordinatesSkewer(float noSkewingDistance)
        {
            _noSkewingDistance = noSkewingDistance;
        }

        public Vector2 UnSkewPoint(Vector2 input)
        {
            return ChangeFrom_minus1_1_to_0_1(UnSkewGlobalToTerrainArrayPos(ChangeFrom_0_1_to_minus1_1(input)));
        }

        public Vector2 SkewPoint(Vector2 input)
        {
            return ChangeFrom_minus1_1_to_0_1(SkewGlobalToTerrainArrayPos(ChangeFrom_0_1_to_minus1_1(input)));
        }

        private Vector2 SkewGlobalToTerrainArrayPos(Vector2 input) // <-1,1>
        {
            Preconditions.Assert(Math.Abs(input.x) <= 1f, "Input has to have values from -1 to 1");
            Preconditions.Assert(Math.Abs(input.y) <= 1f, "Input has to have values from -1 to 1");

            float radius = Math.Max(Math.Abs(input.x), Math.Abs(input.y));
            radius = (float) Math.Pow(radius, 2);

            float noSkewingDistance = Mathf.Pow(_noSkewingDistance, 2);
            //radius = Mathf.Max(0.04f, radius);
            radius = Mathf.Max(noSkewingDistance, radius);

            Vector2 newPosition = input * radius;
            return newPosition;
        }

        private Vector2 UnSkewGlobalToTerrainArrayPos(Vector2 input) // <-1,1>
        {
            Preconditions.Assert(Math.Abs(input.x) <= 1f, "Input has to have values from -1 to 1");
            Preconditions.Assert(Math.Abs(input.y) <= 1f, "Input has to have values from -1 to 1");

            float radius = Math.Max(Math.Abs(input.x), Math.Abs(input.y));
            radius = (float) Math.Sqrt(radius);

            float noSkewingDistance = _noSkewingDistance;
            //radius = Mathf.Max(0.04f, radius);
            radius = Mathf.Max(noSkewingDistance, radius);

            Vector2 newPosition = input * radius;
            return newPosition;
        }

        private Vector2 ChangeFrom_0_1_to_minus1_1(Vector2 input)
        {
            return (input * 2f) - new Vector2(1f, 1f);
        }

        private Vector2 ChangeFrom_minus1_1_to_0_1(Vector2 input)
        {
            return (input + new Vector2(1f, 1f)) / 2f;
        }
    }

    public class StainSpaceToUnitySpaceTranslator
    {
        private MyRectangle _unitySpaceOfStain;

        public StainSpaceToUnitySpaceTranslator(MyRectangle unitySpaceOfStain)
        {
            _unitySpaceOfStain = unitySpaceOfStain;
        }

        public Vector2 Translate(Vector2 uv)
        {
            return RectangleUtils.CalculateSubPosition(_unitySpaceOfStain, uv);
        }

        public static StainSpaceToUnitySpaceTranslator DefaultTranslator => new StainSpaceToUnitySpaceTranslator(
            new MyRectangle(0, 0, 1, 1));
    }

    public class StainTerrainArrayFromBiomesGeneratorConfiguration
    {
        public int TexturesSideLength = 64;
        public int PaletteSamplesCount = 16;
        public int PriorityWeightFactor = 2;
        public float NoSkewingDistance = 0.2f;
    }
}