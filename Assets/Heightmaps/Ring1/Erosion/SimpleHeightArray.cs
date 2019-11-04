using System;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class SimpleHeightArray : MySimpleArray<float>
    {
        public SimpleHeightArray(float[,] array) : base(array)
        {
        }

        public SimpleHeightArray(int x, int y) : base(x, y)
        {
        }

        public static SimpleHeightArray FromHeightmap(HeightmapArray heightmapArray)
        {
            return new SimpleHeightArray(heightmapArray.HeightmapAsArray);
        }

        public static HeightmapArray ToHeightmap(SimpleHeightArray sourceArray)
        {
            return new HeightmapArray(sourceArray._array);
        }

        public void AddValue(IntVector2 point, float value)
        {
            SetValue(point, GetValue(point) + value);
        }

        public void SumValue(SimpleHeightArray localDifferenceArray)
        {
            for (int x = 0; x < localDifferenceArray.Width; x++)
            {
                for (int y = 0; y < localDifferenceArray.Height; y++)
                {
                    AddValue(new IntVector2(x, y), localDifferenceArray.GetValue(x, y));
                }
            }
        }

        public override void SetValue(int x, int y, float value)
        {
            Preconditions.Assert(!float.IsNaN(value), "");
            base.SetValue(x, y, value);
        }

        public float GetValueWithIndexClamped(Vector2 uv)
        {
            return MyArrayUtils.GetValueWithIndexClamped(Array, uv);
        }

        public float GetValueWithZeroInMissing(int x, int y)
        {
            if (Boundaries.AreValidIndexes(new IntVector2(x, y)))
            {
                return GetValue(x, y);
            }
            else
            {
                return 0;
            }
        }
    }
}