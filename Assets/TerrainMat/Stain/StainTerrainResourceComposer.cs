using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.TerrainMat.Stain
{
    public class StainTerrainResourceComposer
    {
        private StainTerrainResourceCreatorUTProxy _creator;

        public StainTerrainResourceComposer(StainTerrainResourceCreatorUTProxy creator)
        {
            _creator = creator;
        }

        public async Task<StainTerrainResource> ComposeAsync(List<ColorPack> paletteArray, int[,] paletteIndexArray,
            Vector4[,] controlArray)
        {
            var paletteColorArray = GenerateTerrainPaletteColorArray(paletteArray);
            var paletteIndexColorArray = GeneratePaletteIndexColorArray(paletteIndexArray, paletteArray.Count);
            var controlColorArray = GenerateControlColorArray(controlArray);
            var generatedResources = await _creator.GenerateResourcesAsync(new StainTerrainResourceTextureTemplate()
            {
                ControlColorArray = controlColorArray,
                PaletteIndexColorArray = paletteIndexColorArray,
                PaletteColorArray = paletteColorArray
            });

            return generatedResources;
        }

        private TwoDArrayAsSingle<Color> GenerateControlColorArray(Vector4[,] controlArray)
        {
            var texturesSideLength = controlArray.GetLength(0);
            var controlColorArray = new TwoDArrayAsSingle<Color>(texturesSideLength, texturesSideLength);
            for (int x = 0; x < texturesSideLength; x++)
            {
                for (int y = 0; y < texturesSideLength; y++)
                {
                    controlColorArray.Set(x, y, PackControlValue(controlArray[x, y]));
                }
            }
            return controlColorArray;
        }

        private TwoDArrayAsSingle<Color> GeneratePaletteIndexColorArray(int[,] paletteIndexArray, int paletteSize)
        {
            var texturesSideLength = paletteIndexArray.GetLength(0);
            var paletteIndexColorArray = new TwoDArrayAsSingle<Color>(texturesSideLength, texturesSideLength);

            for (int x = 0; x < texturesSideLength; x++)
            {
                for (int y = 0; y < texturesSideLength; y++)
                {
                    paletteIndexColorArray.Set(x, y, new Color(paletteIndexArray[x, y] / (float) paletteSize, 0, 0));
                }
            }
            return paletteIndexColorArray;
        }

        private TwoDArrayAsSingle<Color> GenerateTerrainPaletteColorArray(List<ColorPack> paletteArray)
        {
            var paletteColorArray = new TwoDArrayAsSingle<Color>(paletteArray.Count * 4, 1);
            for (int i = 0; i < paletteArray.Count; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    paletteColorArray.Set(i * 4 + k, 0, paletteArray[i][k]);
                }
            }
            return paletteColorArray;
        }


        public static Color PackControlValue(Vector4 input)
        {
            var outColor = new Color(0.0f, 0.0f, 0.0f);
            float upSum = input[0] + input[1];
            if (Math.Abs(upSum) < 0.001)
            {
                outColor[0] = 0.5f;
            }
            else
            {
                outColor[0] = input[0] / upSum;
            }

            float downSum = input[2] + input[3];
            if (Math.Abs(downSum) < 0.001)
            {
                outColor[1] = 0.5f;
            }
            else
            {
                outColor[1] = input[2] / downSum;
            }

            float wholeSum = upSum + downSum;

            if (Math.Abs(wholeSum) < 0.001)
            {
                return outColor;
            }
            outColor[2] = upSum / wholeSum;
            return outColor;
        }
    }
}