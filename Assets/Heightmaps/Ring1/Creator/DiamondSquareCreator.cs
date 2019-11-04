using System;
using Assets.Random;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Creator
{
    public class DiamondSquareCreator
    {
        private RandomProvider randomProvider;
        private int iterationIndex;

        public DiamondSquareCreator(RandomProvider randomProvider)
        {
            this.randomProvider = randomProvider;
        }

        public HeightmapArray AddDetail(HeightmapArray inputArray)
        {
            ExtendedArray extendedArray = new ExtendedArray(inputArray.Width, inputArray.Height);
            InitializeArray(extendedArray);
            iterationIndex = 1;
            for (int sideLength = extendedArray.SideLength() - 1; sideLength >= 2; sideLength /= 2)
            {
                iterationIndex++;
                DiamondStep(extendedArray, sideLength);
                SquareStep(extendedArray, sideLength);
            }

            var outArray = extendedArray.getOriginalSizedArray();
            //NormalizeArray(outArray);
            return new HeightmapArray(outArray);
        }

        public HeightmapArray CreateDiamondSquareNoiseArray(IntVector2 requestedSize, int workingArraySize)
        {
            var fullSize = CalculateFullSize(requestedSize, workingArraySize);

            ExtendedArray extendedArray = new ExtendedArray(fullSize.X, fullSize.Y);
            InitializeArray(extendedArray);

            for (int x = 0; x < requestedSize.X - 1; x += workingArraySize)
            {
                for (int y = 0; y < requestedSize.Y - 1; y += workingArraySize)
                {
                    var wrapper =
                        new SmallWindowExtendedArrayWrapper(extendedArray, new IntVector2(x, y), workingArraySize + 1);
                    iterationIndex = 1;
                    for (int sideLength = wrapper.SideLength() - 1; sideLength >= 2; sideLength /= 2)
                    {
                        iterationIndex++;
                        DiamondStep(wrapper, sideLength);
                        SquareStep(wrapper, sideLength);
                    }
                }
            }

            var outArray = extendedArray.GetSubArray(requestedSize);
            NormalizeArrayWithExtremesChecking(outArray);
            return new HeightmapArray(outArray);
        }

        private IntVector2 CalculateFullSize(IntVector2 requestedSize, int workingArraySize)
        {
            var sizeMinusOne = new IntVector2(requestedSize.X - 1, requestedSize.Y - 1);

            return new IntVector2(
                Mathf.CeilToInt((float) sizeMinusOne.X / workingArraySize) * workingArraySize + 1,
                Mathf.CeilToInt((float) sizeMinusOne.Y / workingArraySize) * workingArraySize + 1);
        }

        private void NormalizeArrayWithExtremesChecking(float[,] outArray)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int x = 0; x < outArray.GetLength(0); x++)
            {
                for (int y = 0; y < outArray.GetLength(1); y++)
                {
                    float val = outArray[x, y];
                    min = Mathf.Min(val, min);
                    max = Mathf.Max(val, max);
                }
            }

            float delta = max - min;
            for (int x = 0; x < outArray.GetLength(0); x++)
            {
                for (int y = 0; y < outArray.GetLength(1); y++)
                {
                    float originalValue = outArray[x, y];
                    outArray[x, y] = (originalValue - min) / delta;
                }
            }
        }

        private void NormalizeArray(float[,] outArray)
        {
            for (int x = 0; x < outArray.GetLength(0); x++)
            {
                for (int y = 0; y < outArray.GetLength(1); y++)
                {
                    outArray[x, y] = (outArray[x, y] + 1) / 2;
                }
            }
        }

        private void InitializeArray(ExtendedArray extendedArray)
        {
            int endIndex = extendedArray.SideLength() - 1;
            float constantStartValue = 0;
            extendedArray.SetValue(0, 0, constantStartValue);
            extendedArray.SetValue(0, endIndex, constantStartValue);
            extendedArray.SetValue(endIndex, 0, constantStartValue);
            extendedArray.SetValue(endIndex, endIndex, constantStartValue);
        }

        private void DiamondStep(IWorkingArray extendedArray, int sideLength)
        {
            for (int x = 0; x < extendedArray.SideLength() - 1; x += sideLength)
            {
                for (int y = 0; y < extendedArray.SideLength() - 1; y += sideLength)
                {
                    double average =
                    (extendedArray.GetValue(x, y) +
                     extendedArray.GetValue(x + sideLength, y) +
                     extendedArray.GetValue(x, y + sideLength) +
                     extendedArray.GetValue(x + sideLength, y + sideLength)) / 4;
                    float value = CalculateNextHeight(average, x + sideLength / 2, y + sideLength / 2);
                    extendedArray.SetValue(x + sideLength / 2, y + sideLength / 2, value);
                }
            }
        }

        private float CalculateNextHeight(double average, int x, int y)
        {
            double multiplier = Math.Pow(2, iterationIndex + 1);
            return (float) (average + (randomProvider.NextValue - 0.5f) / multiplier);
        }

        private void SquareStep(IWorkingArray extendedArray, int sideLength)
        {
            int half = sideLength / 2;
            int wholeLength = extendedArray.SideLength();
            for (int x = 0; x < wholeLength - 1; x += half)
            {
                for (int y = (x + half) % sideLength; y < wholeLength - 1; y += sideLength)
                {
                    double average =
                    (extendedArray.GetValue((x - half + wholeLength - 1) % (wholeLength - 1), y) +
                     extendedArray.GetValue((x + half) % (wholeLength - 1), y) +
                     extendedArray.GetValue(x, (y + half) % (wholeLength - 1)) +
                     extendedArray.GetValue(x, (y - half + wholeLength - 1) % (wholeLength - 1))) / 4;
                    float value = CalculateNextHeight(average, x + sideLength / 2, y + sideLength / 2);
                    extendedArray.SetValue(x + sideLength / 2, y + sideLength / 2, value);
                }
            }
        }

        internal interface IWorkingArray
        {
            int SideLength();
            float GetValue(int x, int y);
            void SetValue(int x, int y, float value);
        }

        internal class SmallWindowExtendedArrayWrapper : IWorkingArray
        {
            private ExtendedArray _internalArray;
            private IntVector2 _offset;
            private int _sideLength;

            public SmallWindowExtendedArrayWrapper(ExtendedArray internalArray, IntVector2 offset, int sideLength)
            {
                _internalArray = internalArray;
                _offset = offset;
                _sideLength = sideLength;
            }

            public int SideLength()
            {
                return _sideLength;
            }

            public float GetValue(int x, int y)
            {
                return _internalArray.GetValue(x + _offset.X, y + _offset.Y);
            }

            public void SetValue(int x, int y, float value)
            {
                Preconditions.Assert(x < _sideLength, "X is too big: " + x);
                Preconditions.Assert(y < _sideLength, "Y is too big: " + y);
                _internalArray.SetValue(x + _offset.X, y + _offset.Y, value);
            }
        }

        internal class ExtendedArray : IWorkingArray
        {
            private readonly int _originalWidth;
            private readonly int _originalHeight;
            private float[,] extendedArray;

            public ExtendedArray(int originalWidth, int originalHeight)
            {
                int biggerLength = Mathf.Max(originalHeight, originalWidth);
                int extendedLength = (int) Math.Round(Mathf.Pow(2, Mathf.Floor(Mathf.Log(biggerLength, 2))) + 1);
                extendedArray = new float[extendedLength, extendedLength];

                _originalWidth = originalWidth;
                _originalHeight = originalHeight;
            }

            public float GetValue(int x, int y)
            {
                return extendedArray[x, y];
            }

            public void SetValue(int x, int y, float value)
            {
                try
                {
                    extendedArray[x, y] = value;
                }
                catch (Exception o)
                {
                    Debug.Log(" BAD x is " + x + " y is " + y);
                    int x3 = 12;
                    throw o;
                }
            }

            public int SideLength()
            {
                return extendedArray.GetLength(0);
            }

            public float[,] getOriginalSizedArray()
            {
                float[,] outArray = new float[_originalWidth, _originalHeight];
                for (int y = 0; y < _originalHeight; y++)
                {
                    Array.Copy(extendedArray, extendedArray.GetLength(0) * y, outArray, outArray.GetLength(0) * y,
                        _originalWidth);
                }
                return outArray;
            }

            public float[,] GetSubArray(IntVector2 size)
            {
                float[,] outArray = new float[size.X, size.Y];
                for (int y = 0; y < size.Y; y++)
                {
                    Array.Copy(extendedArray, extendedArray.GetLength(0) * y, outArray, outArray.GetLength(0) * y,
                        size.X);
                }
                return outArray;
            }
        }
    }
}