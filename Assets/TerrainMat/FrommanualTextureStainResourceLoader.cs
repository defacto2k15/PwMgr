using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.Creator;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class FromManualTextureStainResourceLoader : ITerrainArrayGenerator
    {
        private FromManualTextureStainResourceLoaderConfiguration _configuration;
        private readonly BiomeInstanceDetailGenerator _detailGenerator;

        public FromManualTextureStainResourceLoader(
            BiomeInstanceDetailGenerator detailGenerator,
            FromManualTextureStainResourceLoaderConfiguration configuration)
        {
            _configuration = configuration;
            _detailGenerator = detailGenerator;
        }

        public StainTerrainArray ProvideData()
        {
            var texture = SavingFileManager.LoadPngTextureFromFile(_configuration.ManualTexturePath,
                _configuration.InputTextureSize.X, _configuration.InputTextureSize.Y, TextureFormat.ARGB32, false,
                false); //todo add texture skewing here

            var characteristicsArray = GenerateCharacteristicsArray(texture, _configuration.InputTextureSize);
            var arraySize = _configuration.InputTextureSize;

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

            return new StainTerrainArray(terrainPaletteArray, controlArray, paletteIndexArray);
        }

        private BiomeInstanceCharacteristics[,] GenerateCharacteristicsArray(Texture2D texture, IntVector2 size)
        {
            var outArray = new BiomeInstanceCharacteristics[size.X, size.Y];
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    var texColor = texture.GetPixel(x, y);
                    var biome = FindAccordingBiome(texColor);
                    outArray[x, y] =
                        new BiomeInstanceCharacteristics(biome, new BiomeInstanceId((uint) biome.GetHashCode()), 1);
                }
            }
            return outArray;
        }

        private BiomeType FindAccordingBiome(Color color)
        {
            return _configuration.ControlColorToBiomeTypeDict
                .OrderBy(c => Vector3.Distance(c.Key.ToVector3(), color.ToVector3())).Select(c => c.Value).First();
        }
    }

    public class FromManualTextureStainResourceLoaderConfiguration
    {
        public IntVector2 InputTextureSize;
        public string ManualTexturePath;

        public Dictionary<Color, BiomeType> ControlColorToBiomeTypeDict = new Dictionary<Color, BiomeType>()
        {
            {
                Color.yellow, BiomeType.Forest
            },
            {
                Color.red, BiomeType.NotSpecified
            },
            {
                Color.blue, BiomeType.Scrub
            },
            {
                Color.green, BiomeType.Fell
            },
        };
    }
}