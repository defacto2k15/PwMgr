using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.submaps;

namespace Assets.Heightmaps.Preparment.Slicing
{
    public class TerrainSlicer
    {
        public List<Submap> Slice(HeightmapArray input, List<SubmapPosition> slicingPositions, int inputLodFactor)
        {
            return
                slicingPositions.Select(
                    pos => new Submap(
                        new HeightmapArray(GetSubarrayWithEmptyMargins(input.HeightmapAsArray, pos.DownLeftX,
                            pos.DownLeftY, pos.Width, pos.Height)),
                        pos,
                        inputLodFactor)).ToList();
        }

        private float[,] GetSubarrayWithEmptyMargins(float[,] inputArray, int xOffset, int yOffset,
            int subarrayWorkingWidth, int subarrayWorkingHeight)
        {
            int subarrayHeight = subarrayWorkingHeight + 1;
            int subarrayWidth = subarrayWorkingWidth + 1;

            int inputWidth = inputArray.GetLength(0);
            int inputHeight = inputArray.GetLength(1);
            if (xOffset + subarrayWorkingWidth > inputWidth)
            {
                throw new ArgumentException("xOffset + submapWidth > mapWidth");
            }
            if (yOffset + subarrayWorkingHeight > inputHeight)
            {
                throw new ArgumentException("yOffset + submapHeight > mapWidth");
            }

            float[,] subarray = new float[subarrayWidth, subarrayHeight];
            for (int i = 0; i < subarrayWidth; i++)
            {
                Array.Copy(inputArray, (i + xOffset) * inputWidth + yOffset, subarray, i * subarrayHeight,
                    subarrayHeight);
            }
            return subarray;
        }

        public float[,] GetSubarrayWithEmptyMarginsSafe(float[,] inputArray, int xOffset, int yOffset,
            int subarrayWorkingWidth, int subarrayWorkingHeight)
        {
            int subarrayHeight = subarrayWorkingHeight + 1;
            int subarrayWidth = subarrayWorkingWidth + 1;

            int inputWidth = inputArray.GetLength(0);
            int inputHeight = inputArray.GetLength(1);
            if (xOffset + subarrayWorkingWidth > inputWidth)
            {
                throw new ArgumentException("xOffset + submapWidth > mapWidth");
            }
            if (yOffset + subarrayWorkingHeight > inputHeight)
            {
                throw new ArgumentException("yOffset + submapHeight > mapWidth");
            }

            float[,] subarray = new float[subarrayWidth, subarrayHeight];
            for (int i = 0; i < subarrayWidth - 1; i++)
            {
                Array.Copy(inputArray, (i + xOffset) * inputWidth + yOffset, subarray, i * subarrayHeight,
                    subarrayHeight - 1);
            }
            return subarray;
        }
    }
}