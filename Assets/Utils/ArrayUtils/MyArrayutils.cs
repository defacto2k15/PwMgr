using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Utils.ArrayUtils
{
    public static partial class MyArrayUtils
    {
        public static void PopulateArray<T>(T[,] array, T val)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    array[x, y] = val;
                }
            }
        }

        public static T[,] DeepClone<T>(T[,] orig)
        {
            T[,] newArray = new T[orig.GetLength(0), orig.GetLength(1)];
            for (int x = 0; x < orig.GetLength(0); x++)
            {
                for (int y = 0; y < orig.GetLength(1); y++)
                {
                    newArray[x, y] = orig[x, y];
                }
            }
            return newArray;
        }

        public static float Min(float[,] orig)
        {
            float min = Enumerable.Range(0, orig.GetLength(0)).AsParallel()
                .Min(x => Enumerable.Range(0, orig.GetLength(1)).Min(y => orig[x, y]));
            return min;
        }

        public static float Max(float[,] orig)
        {
            float max = Enumerable.Range(0, orig.GetLength(0)).AsParallel()
                .Max(x => Enumerable.Range(0, orig.GetLength(1)).Max(y => orig[x, y]));
            return max;
        }

        public static ArrayExtremes CalculateExtremes(float[,] orig)
        {
            return new ArrayExtremes(Min(orig), Max(orig));
        }

        public static void Normalize(float[,] orig, ArrayExtremes extremes = null)
        {
            if (extremes == null)
            {
                extremes = CalculateExtremes(orig);
            }
            if (extremes.Delta == 0f)
            {
                return;
            }
            for (int x = 0; x < orig.GetLength(0); x++)
            {
                for (int y = 0; y < orig.GetLength(1); y++)
                {
                    orig[x, y] = (orig[x, y] - extremes.Min) / extremes.Delta;
                }
            }
        }

        public static void Multiply(float[,] orig, float multiplier)
        {
            for (int x = 0; x < orig.GetLength(0); x++)
            {
                for (int y = 0; y < orig.GetLength(1); y++)
                {
                    orig[x, y] *= multiplier;
                }
            }
        }

        public static void InvertNormalized(float[,] orig)
        {
            for (int x = 0; x < orig.GetLength(0); x++)
            {
                for (int y = 0; y < orig.GetLength(1); y++)
                {
                    orig[x, y] *= -1;
                    orig[x, y] += 1;
                }
            }
        }

        public static void Copy(float[,] source, float[,] dest)
        {
            Array.Copy(source, dest, source.Length);
        }

        public static float[,] Resize(float[,] source, IntVector2 newSize)
        {
            var outArray = new float[newSize.X, newSize.Y];
            for (int x = 0; x < source.GetLength(0); x++)
            {
                for (int y = 0; y < source.GetLength(1); y++)
                {
                    outArray[x, y] = source[x, y];
                }
            }
            return outArray;
        }

        public static void DeNormalize(float[,] normalizedOrig, ArrayExtremes extremes)
        {
            if (extremes.Delta == 0f)
            {
                return;
            }
            for (int x = 0; x < normalizedOrig.GetLength(0); x++)
            {
                for (int y = 0; y < normalizedOrig.GetLength(1); y++)
                {
                    normalizedOrig[x, y] = extremes.Min + (normalizedOrig[x, y]) * extremes.Delta;
                }
            }
        }

        public static float GetValueWithIndexClamped(float[,] array, Vector2 index)
        {
            var width = array.GetLength(0);
            var height = array.GetLength(1);

            index = new Vector2(
                Mathf.Clamp(index.x, 0, width - 1),
                Mathf.Clamp(index.y, 0, height - 1)
            );
            index = VectorUtils.MemberwiseDivide(index, new Vector2(width - 1, height - 1));

            var widthMaxIndex = width - 1;
            var heightMaxIndex = height - 1;

            int x1 = (int) Mathf.FloorToInt(index.x * widthMaxIndex); // USED TO BE MIN/MAX, CHECK IT!!!
            int x2 = (int) Mathf.CeilToInt(index.x * widthMaxIndex);
            float x1Weight = 1 - ((index.x * widthMaxIndex) - x1);

            int y1 = (int) Mathf.FloorToInt(index.y * heightMaxIndex);
            int y2 = (int) Mathf.CeilToInt(index.y * heightMaxIndex);
            float y1Weight = 1 - ((index.y * heightMaxIndex) - y1);

            float lerp = 0f;
            lerp = Mathf.Lerp(
                Mathf.Lerp(array[x1, y1], array[x2, y1], x1Weight),
                Mathf.Lerp(array[x1, y2], array[x2, y2], x1Weight),
                y1Weight);
            return lerp;
        }

        public static List<ValueWithWeight<T>> GetValueWithUvClamped<T>(MySimpleArray<T> array, Vector2 uv)
        {
            var width = array.Width;
            var height = array.Height;

            uv = new Vector2(
                Mathf.Clamp01(uv.x),
                Mathf.Clamp01(uv.y)
            );

            var widthMaxIndex = width - 1;
            var heightMaxIndex = height - 1;

            int x1 = (int) Mathf.FloorToInt(uv.x * widthMaxIndex); // USED TO BE MIN/MAX, CHECK IT!!!
            int x2 = (int) Mathf.CeilToInt(uv.x * widthMaxIndex);
            float x1Weight = 1 - ((uv.x * widthMaxIndex) - x1);

            int y1 = (int) Mathf.FloorToInt(uv.y * heightMaxIndex);
            int y2 = (int) Mathf.CeilToInt(uv.y * heightMaxIndex);
            float y1Weight = 1 - ((uv.y * heightMaxIndex) - y1);

            var x1y1Weight = (1 - x1Weight) * (1 - y1Weight);
            var x2y1Weight = (x1Weight) * (1 - y1Weight);
            var x1y2Weight = (1 - x1Weight) * (y1Weight);
            var x2y2Weight = (x1Weight) * (y1Weight);

            return new List<ValueWithWeight<T>>()
            {
                new ValueWithWeight<T>()
                {
                    Value = array.GetValue(x1, y1),
                    Weight = x1y1Weight
                },
                new ValueWithWeight<T>()
                {
                    Value = array.GetValue(x2, y1),
                    Weight = x2y1Weight
                },
                new ValueWithWeight<T>()
                {
                    Value = array.GetValue(x1, y2),
                    Weight = x1y2Weight
                },
                new ValueWithWeight<T>()
                {
                    Value = array.GetValue(x2, y2),
                    Weight = x2y2Weight
                },
            };
        }

        public static float GetValueWithUvClampedComputed(MySimpleArray<float> array, Vector2 uv)
        {
            var weightedList = GetValueWithUvClamped(array, uv);
            return weightedList.Sum(c => c.Value * c.Weight);
        }


        public class ValueWithWeight<T>
        {
            public T Value;
            public float Weight;
        }

        public static float GetValueWith01Uv(float[,] array, Vector2 uv)
        {
            var width = array.GetLength(0);
            var height = array.GetLength(1);

            var widthMaxIndex = width - 1;
            var heightMaxIndex = height - 1;

            int x1 = (int) Mathf.FloorToInt(uv.x * widthMaxIndex); // USED TO BE MIN/MAX, CHECK IT!!!
            int x2 = (int) Mathf.CeilToInt(uv.x * widthMaxIndex);
            float x1Weight = 1 - ((uv.x * widthMaxIndex) - x1);

            int y1 = (int) Mathf.FloorToInt(uv.y * heightMaxIndex);
            int y2 = (int) Mathf.CeilToInt(uv.y * heightMaxIndex);
            float y1Weight = 1 - ((uv.y * heightMaxIndex) - y1);

            float lerp = 0f;
            lerp = Mathf.Lerp(
                Mathf.Lerp(array[x1, y1], array[x2, y1], x1Weight),
                Mathf.Lerp(array[x1, y2], array[x2, y2], x1Weight),
                y1Weight);
            return lerp;
        }

        public static float[,] CreateFilled(int x, int y, float height)
        {
            var arr = new float[x,y];
            PopulateArray(arr, height);
            return arr;
        }
    }
}