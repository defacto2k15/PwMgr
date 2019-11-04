using System;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.TextureUtils;
using Assets.Ring2;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    class HeightmapUtils
    {
        public static HeightmapArray CreateHeightmapArrayFromTexture(Texture2D texture)
        {
            float[,] array = new float[texture.width, texture.height];
            Color[] allPixels = texture.GetPixels();
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.height; x++)
                {
                    array[x, y] = HeightColorTransform.DecodeHeight(allPixels[y * texture.height + x]);
                }
            }
            return new HeightmapArray(array);
        }

        public static Texture2D CreateTextureFromHeightmap(HeightmapArray inputHeightmap)
        {
            var inputTexture = new Texture2D(inputHeightmap.Width, inputHeightmap.Height, TextureFormat.RGBA32, false);

            Color[] textureArray = new Color[inputHeightmap.Width * inputHeightmap.Height];
            for (int y = 0; y < inputHeightmap.Height; y++)
            {
                for (int x = 0; x < inputHeightmap.Width; x++)
                {
                    float pixelHeight = inputHeightmap.GetHeight(x, y);
                    textureArray[y * inputHeightmap.Height + x] = //new Color(0.6f, 0.6f, 0.6f, 0.6f);
                        HeightColorTransform.EncodeHeight(pixelHeight);
                }
            }
            inputTexture.SetPixels(textureArray);
            inputTexture.Apply();
            return inputTexture;
        }

        public static Color[] CreateHeightTextureArray(HeightmapArray inputHeightmap)
        {
            Color[] colorArray = new Color[inputHeightmap.Width * inputHeightmap.Height];
            for (int y = 0; y < inputHeightmap.Height; y++)
            {
                for (int x = 0; x < inputHeightmap.Width; x++)
                {
                    float pixelHeight = inputHeightmap.GetHeight(x, y);
                    colorArray[y * inputHeightmap.Height + x] = HeightColorTransform.EncodeHeight(pixelHeight);
                }
            }
            return colorArray;
        }

        public static Texture2D CreateTextureFromHeightmap_OLD(HeightmapArray inputHeightmap)
        {
            var inputTexture = new Texture2D(inputHeightmap.EvenWidth, inputHeightmap.EvenHeight, TextureFormat.RFloat,
                false);

            float[] textureArray = new float[inputHeightmap.EvenWidth * inputHeightmap.EvenHeight];
            for (int y = 0; y < inputHeightmap.EvenHeight; y++)
            {
                for (int x = 0; x < inputHeightmap.EvenWidth; x++)
                {
                    float pixelHeight = inputHeightmap.GetHeight(x, y);
                    textureArray[y * inputHeightmap.EvenHeight + x] = pixelHeight;
                }
            }

            byte[] rawTextureArray = new byte[sizeof(float) * (inputHeightmap.EvenWidth * inputHeightmap.EvenHeight)];
            System.Buffer.BlockCopy(textureArray, 0, rawTextureArray, 0, rawTextureArray.Length);
            inputTexture.LoadRawTextureData(rawTextureArray);
            inputTexture.Apply();
            return inputTexture;
        }

        public static HeightmapArray SmoothHeightChanges(HeightmapArray afterGenerationHeightmapArray)
        {
            var heightArray = afterGenerationHeightmapArray.HeightmapAsArray;
            var smoothedHeightArray = new float[heightArray.GetLength(0), heightArray.GetLength(1)];
            var smoothingFactors = new SmoothingFactorData[heightArray.GetLength(0), heightArray.GetLength(1)];

            int xStartPoint = 0;
            int xEndPoint = 0;

            for (int y = 0; y < heightArray.GetLength(1); y++)
            {
                float lastValue = 0;
                int flatnessStartIndex = 0;
                float flasnessStartValue = 0;
                for (int x = 0; x < heightArray.GetLength(0); x++)
                {
                    float currentValue = heightArray[x, y];
                    if (Math.Abs(currentValue - lastValue) > 0.001f)
                    {
                        for (int nx = flatnessStartIndex + 1; nx < x; x++)
                        {
                            if (smoothingFactors[nx, y] == null)
                            {
                                smoothingFactors[nx, y] = new SmoothingFactorData();
                            }
                            smoothingFactors[nx, y].SetXSmoothingStart(flasnessStartValue, flatnessStartIndex);
                            smoothingFactors[nx, y].SetXSmoothingEnd(currentValue, x);
                        }
                        flatnessStartIndex = x;
                        flasnessStartValue = currentValue;
                    }
                }
            }

            for (int x = 0; x < heightArray.GetLength(0); x++)
            {
                for (int y = 0; y < heightArray.GetLength(1); y++)
                {
                    if (smoothingFactors[x, y] != null)
                    {
                        smoothedHeightArray[x, y] = smoothingFactors[x, y].CalculateSmoothedHeight(x, y);
                    }
                    else
                    {
                        smoothedHeightArray[x, y] = heightArray[x, y];
                    }
                }
            }
            return new HeightmapArray(smoothedHeightArray);
        }

        private class SmoothingFactorData
        {
            private float _flatnessStartValue;
            private int _flatnessStartIndex;
            private float _flatnessEndValue;
            private int _flatnessEndIndex;

            public void SetXSmoothingStart(float flatnessStartValue, int flatnessStartIndex)
            {
                this._flatnessStartValue = flatnessStartValue;
                this._flatnessStartIndex = flatnessStartIndex;
            }

            public void SetXSmoothingEnd(float flatnessEndValue, int flatnessEndIndex)
            {
                this._flatnessEndValue = flatnessEndValue;
                this._flatnessEndIndex = flatnessEndIndex;
            }

            public float CalculateSmoothedHeight(int currentX, int currentY)
            {
                float percent = ((float) currentX - _flatnessStartIndex) /
                                ((float) _flatnessEndIndex - _flatnessStartIndex);
                return Mathf.Lerp(_flatnessStartValue, _flatnessEndValue, percent);
            }
        }

        private class SmoothingStartInfo
        {
            public int DistanceToStartPoint;
            public float PointValue;

            public SmoothingStartInfo(int distanceToStartPoint, float pointValue)
            {
                this.DistanceToStartPoint = distanceToStartPoint;
                this.PointValue = pointValue;
            }
        }

        public static HeightmapArray Resize(HeightmapArray inputHeightmap, int newWidth, int newHeight) //todo delete
        {
            if (inputHeightmap.Width == newWidth && inputHeightmap.Height == newHeight)
            {
                return inputHeightmap;
            }

            Preconditions.Assert(newWidth % inputHeightmap.Width == 0, "New width is not multiplication of old width");
            Preconditions.Assert(newHeight % inputHeightmap.Height == 0,
                "New height is not multiplication of old width");

            float[,] oldHeightmap = inputHeightmap.HeightmapAsArray;
            float[,] newHeightmap = new float[newWidth, newHeight];
            for (int x = 0; x < inputHeightmap.Width; x++) //todo pararelization
            {
                for (int y = 0; y < inputHeightmap.Height; y++)
                {
                }
            }
            throw new NotImplementedException("TODO");
        }

        public static HeightmapArray CutSubArray(HeightmapArray input, IntArea area)
        {
            float[,] newArray = new float[area.Width, area.Height];
            float[,] oldArray = input.HeightmapAsArray;
            for (int y = 0; y < area.Height; y++)
            {
                Array.Copy(oldArray, (y + area.Y) * input.Width + area.X, newArray, y * area.Width, area.Width);
            }
            return new HeightmapArray(newArray);
        }

        public static Texture CreateNormalTexture(Vector3[,] normalArray)
        {
            int width = normalArray.GetLength(0);
            int height = normalArray.GetLength(1);
            var colorArray = new Color[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var changedNormal = normalArray[x, y].Divide(2f).Add(0.5f);
                    colorArray[y * width + x] = new Color(changedNormal.x, changedNormal.y, changedNormal.z);
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
            texture.SetPixels(colorArray);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        public static MySimpleArray<float> EncodedHeightToArray(Texture2D heightTex)
        {
            var heightMap = new float[heightTex.width, heightTex.height];
            for (int x = 0; x < heightTex.width; x++)
            {
                for (int y = 0; y < heightTex.height; y++)
                {
                    if (x == 15 && y == 15)
                    {
                        int a44 = 2;
                    }
                    var pixel = heightTex.GetPixel(x, y);

                    heightMap[x, y] = pixel.r + pixel.g / 255f;
                }
            }

            return new MySimpleArray<float>(heightMap);
        }
    }
}