using System.Linq;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Measuring.Gauges
{
    static class GaugeUtils
    {
        public static bool PixelLiesOverHatchInShape(Color pix)
        {
            return pix.r > 0.5;
        }

        public static bool PixelLiesOverShape(Color pix)
        {
            return pix.a > 0.5;
        }

        public static bool PixelLiesOverShapeAndHatch(Color pix)
        {
            return PixelLiesOverShape(pix) && PixelLiesOverHatchInShape(pix);
        }

        public static uint RetriveId(Color pix)
        {
            return ((uint) Mathf.RoundToInt(pix.r * 255))
                   + ((uint) Mathf.RoundToInt(pix.g * 255 * 256))
                   + ((uint) Mathf.RoundToInt(pix.b * 255 * 256 * 256))
                   + ((uint) Mathf.RoundToInt(pix.a * 255 * 256 * 256 * 256));
        }

        public static uint[,] GenerateIdArray(LocalTexture idTexture)
        {
            var imageSize = new IntVector2(idTexture.Width, idTexture.Height);
            var outIdArray = new uint[imageSize.X, imageSize.Y];

            Parallel.For(0, imageSize.X, x => 
                //for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    outIdArray[x, y] = GaugeUtils.RetriveId(idTexture.GetPixel(x, y));
                }
            });

            //var xc = Enumerable.Range(0, outIdArray.GetLength(0))
            //    .Any(x => Enumerable.Range(0, outIdArray.GetLength(1)).Any(y => outArray[x, y]));
            return outIdArray;
        }

        public static bool[,] GenerateHatchOccurenceArray(LocalTexture mainTexture)
        {
            var imageSize = new IntVector2(mainTexture.Width, mainTexture.Height);
            var outArray = new bool[imageSize.X, imageSize.Y];

            Parallel.For(0, imageSize.X, x => 
                //for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    outArray[x, y] = GaugeUtils.PixelLiesOverShapeAndHatch(mainTexture.GetPixel(x, y));
                }
            });

            //var xc = Enumerable.Range(0, outArray.GetLength(0))
            //    .Any(x => Enumerable.Range(0, outArray.GetLength(1)).Any(y => outArray[x, y]));
            return  outArray;
        }
    }
}