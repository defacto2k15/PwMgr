using System;
using System.Collections.Generic;
using Assets.Random;
using Assets.Utils;
using UnityEngine;

namespace Assets.TerrainMat.Stain
{
    internal class DummyStainTerrainArrayFromBiomesGenerator : ITerrainArrayGenerator
    {
        private readonly BiomeInstancesContainer _biomeInstancesContainer;
        private readonly RandomProvider _randomProvider = new RandomProvider(555); //todo!
        private readonly StainTerrainArrayFromBiomesGeneratorConfiguration _configuration;

        public DummyStainTerrainArrayFromBiomesGenerator(BiomeInstancesContainer biomeInstancesContainer,
            StainTerrainArrayFromBiomesGeneratorConfiguration configuration)
        {
            _biomeInstancesContainer = biomeInstancesContainer;
            _configuration = configuration;
        }

        public StainTerrainArray ProvideData()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("TerrainPaletteArray");
            var paletteArray = GenerateTerrainPaletteArray();
            msw.StartSegment("ControlArray");
            var controlArray = GenerateControlArray(_configuration.TexturesSideLength);
            msw.StartSegment("PaletteIndex");
            var paletteIndexArray = GeneratePaletteIndex(_configuration.TexturesSideLength, _biomeInstancesContainer);
            Debug.Log("T65 after generation: " + msw.CollectResults());

            return new StainTerrainArray(paletteArray, controlArray, paletteIndexArray);
        }

        private List<ColorPack> GenerateTerrainPaletteArray()
        {
            var palette = new List<ColorPack>();

            for (var i = 0; i < _configuration.PaletteSamplesCount; i++)
            {
                var array = new Color[4];
                for (var k = 0; k < 4; k++)
                {
                    array[k] =
                        new Color(
                            Mathf.Repeat(_randomProvider.NextValue, 0.1f) * 10f,
                            Mathf.Repeat(_randomProvider.NextValue, 0.1f) * 10f,
                            Mathf.Repeat(_randomProvider.NextValue, 0.1f) * 10f
                        );
                }
                palette.Add(new ColorPack(array));
            }
            palette[0] = new ColorPack(
                new[]
                {
                    new Color(1.0f, 0.0f, 0.0f),
                    new Color(0.0f, 1.0f, 0.0f),
                    new Color(0.0f, 0.0f, 1.0f),
                    new Color(0.0f, 0.0f, 0.0f)
                });
            palette[1] = new ColorPack(
                new[]
                {
                    new Color(1.0f, 1.0f, 0.0f),
                    new Color(1.0f, 0.0f, 1.0f),
                    new Color(0.5f, 0.5f, 0.5f),
                    new Color(0.0f, 0.0f, 0.0f)
                });
            return palette;
        }

        private Vector4[,] GenerateControlArray(int texturesSideLength)
        {
            var width = texturesSideLength;
            var height = texturesSideLength;

            var controlArray = new Vector4[width, height];
            for (var x = 0; x < width; x++)
            {
                var control =
                    new Vector4(
                        0.5f, 0.5f, 0.5f, 0.5f
                    );
                for (var y = 0; y < height; y++)
                {
                    controlArray[x, y] = control;
                }
            }
            return controlArray;
        }

        private int[,] GeneratePaletteIndex(int texturesSideLength, BiomeInstancesContainer biomeInstancesContainer)
        {
            var width = texturesSideLength;
            var height = texturesSideLength;

            var paletteIndex = new int[texturesSideLength, texturesSideLength];

            List<MyPolygon> queryPolygons = new List<MyPolygon>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var queryPolygon = new MyPolygon(new[]
                    {
                        SkewPoint(new Vector2((float) x / width, (float) y / height)),
                        SkewPoint(new Vector2((float) (x + 1) / width, (float) y / height)),
                        SkewPoint(new Vector2((float) (x + 1) / width, (float) (y + 1) / height)),
                        SkewPoint(new Vector2((float) x / width, (float) (y + 1) / height)),
                    });
                    queryPolygons.Add(queryPolygon);
                }
            }

            var biomes = biomeInstancesContainer.BulkGetBiomeTypesIn(queryPolygons);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var index = 0;
                    var biomeType = biomes[y * width + x][0].Characteristics.Type;

                    if (biomeType == BiomeType.Forest)
                    {
                        index = 1;
                    }
                    else if (biomeType == BiomeType.Grass)
                    {
                        index = 0;
                    }
                    else if (biomeType == BiomeType.Sand)
                    {
                        index = 2;
                    }
                    else
                    {
                        index = 0;
                    }

                    paletteIndex[x, y] = index;
                }
            }

            return paletteIndex;
        }

        public Vector2 SkewPoint(Vector2 input)
        {
            return ChangeFrom_minus1_1_to_0_1(SkewGlobalToTerrainArrayPos(ChangeFrom_0_1_to_minus1_1(input)));
        }

        public Vector2 SkewGlobalToTerrainArrayPos(Vector2 input) // <-1,1>
        {
            Preconditions.Assert(Math.Abs(input.x) <= 1f, "Input has to have values from -1 to 1");
            Preconditions.Assert(Math.Abs(input.y) <= 1f, "Input has to have values from -1 to 1");

            float radius = Math.Max(Math.Abs(input.x), Math.Abs(input.y));
            radius = (float) Math.Pow(radius, 2);
            radius = Mathf.Max(0.04f, radius);

            Vector2 newPosition = input * radius;
            return newPosition;
        }

        public Vector2 ChangeFrom_0_1_to_minus1_1(Vector2 input)
        {
            return (input * 2f) - new Vector2(1f, 1f);
        }

        public Vector2 ChangeFrom_minus1_1_to_0_1(Vector2 input)
        {
            return (input + new Vector2(1f, 1f)) / 2f;
        }
    }
}