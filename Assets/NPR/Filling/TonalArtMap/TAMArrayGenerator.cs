using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMArrayGenerator
    {
        public Texture2DArray Generate(TAMSoleImagesPack pack, List<TAMTone> tones, List<TAMMipmapLevel> levels)
        {
            var levelsCount = levels.Count;
            var tonesCount = tones.Count;
            var maxResolution = new IntVector2(
                pack.Columns[tones[0]][levels[levelsCount - 1]].width,
                pack.Columns[tones[0]][levels[levelsCount - 1]].height);

            var tamTexture = new Texture2DArray(maxResolution.X, maxResolution.Y, tonesCount, TextureFormat.RGBA32, true,true);
            var currentResolution = maxResolution;

            for (var levelIndex = 0; levelIndex < levelsCount; levelIndex++)
            {
                for (int toneIndex = 0; toneIndex < tonesCount; toneIndex++)
                {
                    var array = new Color[currentResolution.X * currentResolution.Y];
                    for (int x = 0; x < currentResolution.X; x++)
                    {
                        for (int y = 0; y < currentResolution.Y; y++)
                        {
                            Color newColor = pack.Columns[tones[toneIndex]][levels[levelsCount - 1 - levelIndex]].GetPixel(x, y);
                            array[x + (y * currentResolution.X)] = newColor;
                        }
                    }
                    tamTexture.SetPixels(array,toneIndex,levelIndex);
                }
                currentResolution = new IntVector2(currentResolution.X / 2, currentResolution.Y/2);
            }

            var lowerLevelTam = CreateAutomaticGeneratedLowerMipmaps(currentResolution*2, tones,
                pack.Columns.ToDictionary(
                    p => p.Key,
                    p => p.Value[levels[0]]
                ));

            var neededMipLevelsCount = (int)Mathf.Log(currentResolution.X, 2);
            for (var levelIndex = 0; levelIndex < neededMipLevelsCount; levelIndex++)
            {
                for (int toneIndex = 0; toneIndex < tonesCount; toneIndex++)
                {
                    tamTexture.SetPixels32(lowerLevelTam.GetPixels32(toneIndex, levelIndex+1), toneIndex, levelIndex + levelsCount);
                }
            }

            tamTexture.Apply(false);
            return tamTexture;
        }

        private Texture2DArray CreateAutomaticGeneratedLowerMipmaps(IntVector2 lowestLevelResolution, List<TAMTone> tones,
            Dictionary<TAMTone, Texture2D> lastLevelImages)
        {
            var tonesCount = tones.Count;
            var tamTexture = new Texture2DArray(lowestLevelResolution.X, lowestLevelResolution.Y, tonesCount, TextureFormat.Alpha8, true, true);
            for (int toneIndex = 0; toneIndex < tonesCount; toneIndex++)
            {
                var array = new Color[lowestLevelResolution.X * lowestLevelResolution.Y];
                for (int x = 0; x < lowestLevelResolution.X; x++)
                {
                    for (int y = 0; y < lowestLevelResolution.Y; y++)
                    {
                        Color newColor = lastLevelImages[tones[toneIndex]].GetPixel(x, y);
                        array[x + (y * lowestLevelResolution.X)] = newColor;
                    }
                }

                tamTexture.SetPixels(array, toneIndex);
            }
            tamTexture.Apply(true);
            return tamTexture;
        }
    }

    public class TamIdArrayGenerator
    {
        public Texture2DArray Generate(TamIdSoleImagesPack pack, List<TAMTone> tones, List<TAMMipmapLevel> levels, int layersCount)
        {
            var mipmapLevelsCount = levels.Count;
            var tonesCount = tones.Count;
            var maxResolution = new IntVector2(
                pack.Columns[tones[0]][levels[mipmapLevelsCount - 1]][0].width,
                pack.Columns[tones[0]][levels[mipmapLevelsCount - 1]][0].height);

            var slicesCount = tonesCount * layersCount;
            var tamIdTexture = new Texture2DArray(maxResolution.X, maxResolution.Y, slicesCount, TextureFormat.RGBA32, true, true)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            var currentResolution = maxResolution;

            for (var mipmapLevelIndex = 0; mipmapLevelIndex < mipmapLevelsCount; mipmapLevelIndex++)
            {
                for (int toneIndex = 0; toneIndex < tonesCount; toneIndex++)
                {
                    for (int layerIndex = 0; layerIndex < layersCount; layerIndex++)
                    {
                        var array = new Color[currentResolution.X * currentResolution.Y];
                        for (int x = 0; x < currentResolution.X; x++)
                        {
                            for (int y = 0; y < currentResolution.Y; y++)
                            {
                                Color newColor = pack.Columns[tones[toneIndex]][levels[mipmapLevelsCount - 1 - mipmapLevelIndex]][layerIndex].GetPixel(x, y);
                                array[x + (y * currentResolution.X)] = newColor;
                            }
                        }

                        tamIdTexture.SetPixels(array, toneIndex * layersCount + layerIndex, mipmapLevelIndex);
                    }
                }

                currentResolution = new IntVector2(currentResolution.X / 2, currentResolution.Y / 2);
            }

            var neededMipLevelsCount = (int) Mathf.Log(currentResolution.X, 2);
            for (int toneIndex = 0; toneIndex < tonesCount; toneIndex++)
            {
                for (int layerIndex = 0; layerIndex < layersCount; layerIndex++)
                {
                    var depthIndex = toneIndex * layersCount + layerIndex;
                    var lastSetMipmapIndex = mipmapLevelsCount - 1;
                    var lowerLevelMipmapTexture =
                        CreateAutomaticGeneratedLowerMipmaps(currentResolution * 2, tamIdTexture.GetPixels(depthIndex, lastSetMipmapIndex));

                    for (var mipmapLevelIndex = 0; mipmapLevelIndex < neededMipLevelsCount; mipmapLevelIndex++)
                    {
                        tamIdTexture.SetPixels32(lowerLevelMipmapTexture.GetPixels32(mipmapLevelIndex + 1), depthIndex, mipmapLevelIndex + mipmapLevelsCount);
                    }
                }
            }

            tamIdTexture.Apply(false);
            return tamIdTexture;
        }

        private Texture2D CreateAutomaticGeneratedLowerMipmaps(IntVector2 lowestLevelResolution, Color[] lowestLevelColors)
        {
            var tamTexture = new Texture2D(lowestLevelResolution.X, lowestLevelResolution.Y, TextureFormat.RGBA32, true, true);
            tamTexture.SetPixels(lowestLevelColors);
            tamTexture.Apply();
            return tamTexture;
        }
    }
}