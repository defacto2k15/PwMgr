using Assets.Utils;

namespace Assets.Heightmaps.Ring1.Ring1HeightArrayModifier
{
    public static class BasicHeightmapModifier
    {
        public static HeightmapArray Multiply(float multiplier, HeightmapArray inputArray)
        {
            float[,] newArray = new float[inputArray.Width, inputArray.Height];
            for (int x = 0; x < inputArray.Width; x++)
            {
                for (int y = 0; y < inputArray.Height; y++)
                {
                    newArray[x, y] = inputArray.HeightmapAsArray[x, y] * multiplier;
                }
            }
            return new HeightmapArray(newArray);
        }

        public static HeightmapArray AddConstant(float valueToAdd, HeightmapArray inputArray)
        {
            float[,] newArray = new float[inputArray.Width, inputArray.Height];
            for (int x = 0; x < inputArray.Width; x++)
            {
                for (int y = 0; y < inputArray.Height; y++)
                {
                    newArray[x, y] = inputArray.HeightmapAsArray[x, y] + valueToAdd;
                }
            }
            return new HeightmapArray(newArray);
        }

        public static HeightmapArray AddAnotherHeightmap(HeightmapArray inputArray, HeightmapArray addedArray,
            float addedConstantValue = 0, float multiplier = 1)
        {
            Preconditions.Assert(inputArray.Width == addedArray.Width, "Width of both arrays is not equal");
            Preconditions.Assert(inputArray.Height == addedArray.Height, "Height of both arrays is not equal");

            float[,] newArray = new float[inputArray.Width, inputArray.Height];
            for (int x = 0; x < inputArray.Width; x++)
            {
                for (int y = 0; y < inputArray.Height; y++)
                {
                    newArray[x, y] = inputArray.HeightmapAsArray[x, y] +
                                     (addedArray.HeightmapAsArray[x, y] * multiplier) + addedConstantValue;
                }
            }
            return new HeightmapArray(newArray);
        }
    }
}