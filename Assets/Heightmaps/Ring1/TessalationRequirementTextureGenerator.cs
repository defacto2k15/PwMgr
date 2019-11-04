using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    class TessalationRequirementTextureGenerator
    {
        public TessalationRequirementTexture GenerateTessalationRequirementTexture(HeightmapArray heightmap,
            int reqSquareSize)
        {
            int outTextureSize = heightmap.Height / reqSquareSize;

            RawRFloatTextureBytes textureBytes = new RawRFloatTextureBytes(outTextureSize);
            float[,] heightArr = heightmap.HeightmapAsArray;

            float globalMin = float.MaxValue;
            float globalMax = float.MinValue;

            for (int xTexture = 1; xTexture < outTextureSize - 1; xTexture++)
            {
                for (int yTexture = 1; yTexture < outTextureSize - 1; yTexture++)
                {
                    float deltaSum = 0;
                    int deltaElements = 0;
                    float maxDelta = 0;

                    for (int sqX = 0; sqX < reqSquareSize; sqX++)
                    {
                        for (int sqY = 0; sqY < reqSquareSize; sqY++)
                        {
                            int x = xTexture * reqSquareSize + sqX;
                            int y = yTexture * reqSquareSize + sqY;

                            if (x > 0 && x < heightmap.Width - 1)
                            {
                                try
                                {
                                    float leftXDelta = Math.Abs(heightArr[x, y] - heightArr[x - 1, y]);
                                    float rightXDelta = Math.Abs(heightArr[x, y] - heightArr[x + 1, y]);
                                    float xDelta = Math.Abs(leftXDelta - rightXDelta);
                                    deltaSum += xDelta;
                                    maxDelta = Math.Max(maxDelta, xDelta);
                                    deltaElements++;
                                }
                                catch (IndexOutOfRangeException e)
                                {
                                    int xx = 22;
                                }
                            }

                            if (y > 0 && y < heightmap.Height - 1)
                            {
                                float leftYDelta = Math.Abs(heightArr[x, y] - heightArr[x, y - 1]);
                                float rightYDelta = Math.Abs(heightArr[x, y] - heightArr[x, y + 1]);
                                float yDelta = Math.Abs(leftYDelta - rightYDelta);
                                deltaSum += yDelta;
                                maxDelta = Math.Max(maxDelta, yDelta);
                                deltaElements++;
                            }
                        }
                    }

                    float avgDelta = deltaSum / deltaElements;
                    float valueToSet = avgDelta;
                    //if (maxDelta > avgDelta*1.8)
                    //{
                    //    valueToSet = maxDelta;
                    //}

                    textureBytes.SetPixel(xTexture, yTexture, valueToSet);
                    globalMax = Mathf.Max(valueToSet, globalMax);
                    globalMin = Mathf.Min(valueToSet, globalMin);
                }
            }

            // now, lets normalize array
            for (int x = 0; x < outTextureSize; x++)
            {
                for (int y = 0; y < outTextureSize; y++)
                {
                    float normalized = (textureBytes.GetPixel(x, y) - globalMin) / (globalMax - globalMin);
                    textureBytes.SetPixel(x, y, normalized);
                }
            }


            textureBytes.InitializeTexture();
            textureBytes.ApplyTexture();
            Texture2D tex = textureBytes.GetTexture();

            return new TessalationRequirementTexture(tex);
        }
    }
}