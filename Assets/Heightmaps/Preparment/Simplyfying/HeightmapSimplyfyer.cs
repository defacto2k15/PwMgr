using System;
using Assets.Heightmaps.submaps;
using Assets.Utils;

namespace Assets.Heightmaps.Preparment.Simplyfying
{
    public class HeightmapSimplyfyer
    {
        public Submap SimplyfySubmap(Submap submap, int lodChangeFactor)
        {
            Preconditions.Assert(lodChangeFactor >= 0, "lodChangeFactor is too small");
            if (lodChangeFactor == 0)
            {
                return submap;
            }

            var inputArray = submap.Heightmap.HeightmapAsArray;
            int timesSmallerNewMapWillBe = (int) Math.Round(Math.Pow(2, lodChangeFactor - 1));
            int newWorkingWidth = (inputArray.GetLength(0) - 1) / timesSmallerNewMapWillBe;
            int newWorkingHeight = (inputArray.GetLength(1) - 1) / timesSmallerNewMapWillBe;

            Preconditions.Assert(newWorkingHeight >= 4, "after symplyfing workingHeight would be too small");
            Preconditions.Assert(newWorkingWidth >= 4, "after symplyfing workingWidth would be too small");

            float[,] arrayAfterSymplifying =
                SimplyfyByBlockAverageIgnoreMergeMargins(inputArray, newWorkingWidth, newWorkingHeight);
            return new Submap(new HeightmapArray(arrayAfterSymplifying), submap.SubmapPosition,
                submap.LodFactor + (lodChangeFactor - 1));
        }

        public HeightmapArray SimplyfyHeightmap(HeightmapArray heightmap, int newWorkingWidth, int newWorkingHeight)
        {
            Preconditions.Assert(heightmap.HasMargin, "heightmap Does Not Have margins");
            float[,] newHeightmap = SimplyfyByBlockAverageIgnoreMergeMargins(heightmap.HeightmapAsArray,
                newWorkingWidth,
                newWorkingHeight);
            return new HeightmapArray(newHeightmap);
        }

        public HeightmapArray SimplyfyHeightmapNoMargins(HeightmapArray heightmap, int newWidth, int newHeight)
        {
            Preconditions.Assert(!heightmap.HasMargin, "heightmap Does Have margins");
            float[,] newHeightmap = SimplyfyByBlockAverageNoMargins(heightmap.HeightmapAsArray, newWidth,
                newHeight);
            return new HeightmapArray(newHeightmap);
        }

        private float[,] SimplyfyByBlockAverageNoMargins(float[,] inputArray, int newWidth, int newHeight)
        {
            int oldWidth = inputArray.GetLength(0);
            int oldHeight = inputArray.GetLength(1);


            if (newWidth == oldWidth && newHeight == oldHeight)
            {
                return inputArray;
            }

            int newPixelWidth = oldWidth / newWidth;
            int newPixelHeight = oldHeight / newHeight;

            var newHeightmap = new float[newWidth, newHeight];

            for (int i = 0; i < newWidth; i++)
            {
                for (int j = 0; j < newHeight; j++)
                {
                    newHeightmap[i, j] = SubarraySum(inputArray, i, j, newPixelWidth, newPixelHeight);
                }
            }
            return newHeightmap;
        }


        private float[,] SimplyfyByBlockAverageIgnoreMergeMargins(float[,] inputArray, int newWorkingWidth,
            int newWorkingHeight)
        {
            int WorkingWidth = inputArray.GetLength(0) - 1;
            int WorkingHeight = inputArray.GetLength(1) - 1;


            if (newWorkingWidth == WorkingWidth && newWorkingHeight == WorkingHeight)
            {
                return inputArray;
            }

            int newPixelWidth = WorkingWidth / newWorkingWidth;
            int newPixelHeight = WorkingHeight / newWorkingHeight;

            var newHeightmap = new float[newWorkingWidth + 1, newWorkingHeight + 1];

            for (int i = 0; i < newWorkingWidth; i++)
            {
                for (int j = 0; j < newWorkingHeight; j++)
                {
                    newHeightmap[i, j] = SubarraySum(inputArray, i, j, newPixelWidth, newPixelHeight);
                }
            }
            return newHeightmap;
        }

        private float SubarraySum(float[,] inputArray, int i, int j, int subarrayWidth,
            int subarrayHeight) //todo delete`
        {
            float sum = 0;
            for (int k = 0; k < subarrayWidth; k++)
            {
                for (int l = 0; l < subarrayHeight; l++)
                {
                    sum += inputArray[i * subarrayWidth + k, j * subarrayHeight + l];
                }
            }
            return sum / (subarrayWidth * subarrayHeight);
        }
    }
}