using System;
using System.Linq;
using Assets.Heightmaps.Preparment.MarginMerging;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps
{
    public class HeightmapArray
    {
        private float[,] _array;

        public HeightmapArray(float[,] array)
        {
            this._array = array;
        }

        public int Width
        {
            get { return _array.GetLength(0); }
        }

        public int WorkingWidth
        {
            get { return Width - 1; }
        }

        public int Height
        {
            get { return _array.GetLength(1); }
        }

        public int WorkingHeight
        {
            get { return Height - 1; }
        }

        public int EvenWidth
        {
            get
            {
                if (HasMargin)
                {
                    return WorkingWidth;
                }
                else
                {
                    return Width;
                }
            }
        }

        public int EvenHeight
        {
            get
            {
                if (HasMargin)
                {
                    return WorkingHeight;
                }
                else
                {
                    return Height;
                }
            }
        }

        public float[,] HeightmapAsArray
        {
            get { return _array; }
        }

        public void SetHeight(int x, int y, float newValue)
        {
            _array[x, y] = newValue;
        }

        public float GetHeight(int x, int y)
        {
            return _array[x, y];
        }

        public bool HasMargin
        {
            get { return (WorkingWidth % 2 == 0) && (WorkingHeight % 2 == 0); }
        }

        public HeightmapMargin GetLeftMargin()
        {
            return GetMargin((i) => _array[0, i], Height);
        }

        public HeightmapMargin GetRightMargin()
        {
            return GetMargin((i) => _array[WorkingWidth, i], Height);
        }

        public HeightmapMargin GetDownMargin()
        {
            return GetMargin((i) => _array[i, 0], Width);
        }

        public HeightmapMargin GetTopMargin()
        {
            return GetMargin((i) => _array[i, WorkingHeight], Width);
        }

        private HeightmapMargin GetMargin(Func<int, float> elementGetter, int marginLength)
        {
            float[] outArray = new float[marginLength];
            for (int i = 0; i < marginLength; i++)
            {
                outArray[i] = elementGetter.Invoke(i);
            }
            return new HeightmapMargin(outArray);
        }

        public void SetRightMargin(HeightmapMargin margin)
        {
            AssertMarginHasProperLength(margin, WorkingHeight);
            for (int i = 0; i < Height; i++)
            {
                _array[WorkingWidth, i] = margin.MarginValues[i];
            }
        }

        public void SetLeftMargin(HeightmapMargin margin)
        {
            AssertMarginHasProperLength(margin, WorkingHeight);
            for (int i = 0; i < Height; i++)
            {
                _array[0, i] = margin.MarginValues[i];
            }
        }

        public void SetTopMargin(HeightmapMargin margin)
        {
            AssertMarginHasProperLength(margin, WorkingWidth);
            for (int i = 0; i < Width; i++)
            {
                _array[i, WorkingHeight] = margin.MarginValues[i];
            }
        }

        public void SetBottomMargin(HeightmapMargin margin)
        {
            AssertMarginHasProperLength(margin, WorkingWidth);
            for (int i = 0; i < Width; i++)
            {
                _array[i, 0] = margin.MarginValues[i];
            }
        }

        private void AssertMarginHasProperLength(HeightmapMargin margin, int workingLength)
        {
            Preconditions.Assert(margin.WorkingLength == workingLength,
                "Cant set margin. It has wrong length. Old working length " + workingLength + " new working length " +
                margin.WorkingLength);
        }

        public void SetDownLeftApexMarginHeight(float apexValue, int pixelSize)
        {
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(pixelSize, 0), (float) i / pixelSize);
                }
                SetHeight(i, 0, value);
                SetHeight(0, i, value);
            }
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(0, pixelSize), (float) i / pixelSize);
                }
                SetHeight(0, i, value);
            }
        }

        public void SetDownRightApexMarginHeight(float apexValue, int pixelSize)
        {
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(WorkingWidth - pixelSize, 0), (float) i / pixelSize);
                }
                SetHeight(WorkingWidth - i, 0, value);
            }
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(WorkingWidth, pixelSize), (float) i / pixelSize);
                }
                SetHeight(WorkingWidth, i, value);
            }
        }

        public void SetTopLeftApexMarginHeight(float apexValue, int pixelSize)
        {
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(0, WorkingHeight - pixelSize), (float) i / pixelSize);
                }
                SetHeight(0, WorkingHeight - i, value);
            }
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(pixelSize, WorkingHeight), (float) i / pixelSize);
                }
                SetHeight(i, WorkingHeight, value);
            }
        }

        public void SetTopRightApexMarginHeight(float apexValue, int pixelSize)
        {
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(WorkingWidth - pixelSize, WorkingHeight),
                        (float) i / pixelSize);
                }
                SetHeight(WorkingWidth - i, WorkingHeight, value);
            }
            for (int i = 0; i < pixelSize; i++)
            {
                var value = apexValue;
                if (pixelSize != 0)
                {
                    value = Mathf.Lerp(apexValue, GetHeight(WorkingWidth, WorkingHeight - pixelSize),
                        (float) i / pixelSize);
                }
                SetHeight(WorkingWidth, WorkingHeight - i, value);
            }
        }

        public ValueRange GetHeightvaluesRange()
        {
            return new ValueRange(_array.Cast<float>().Min(), _array.Cast<float>().Max());
        }

        //public HeightmapArray GetSubHeightmap(int xOffset, int yOffset, int submapWidth, int submapHeight ) //todo delete
        //{
        //    if (xOffset + submapWidth > Width)
        //    {
        //        throw new ArgumentException("xOffset + submapWidth > mapWidth");
        //    }
        //    if (yOffset + submapHeight > Height)
        //    {
        //        throw new ArgumentException("yOffset + submapHeight > mapWidth");
        //    }

        //    float[,] heightSubmap = new float[submapWidth, submapHeight];
        //    for (int i = 0; i <  submapWidth; i++)
        //    {
        //        Array.Copy(_array, (i + xOffset) * Width + yOffset, heightSubmap, i * Height, submapHeight);
        //    }
        //    return new HeightmapArray(heightSubmap);
        //}
    }
}