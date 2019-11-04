using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using UnityEngine;

namespace Assets.NPR.Filling.TonalArtMap
{
    public class StrokeImageTParamSetterGO : MonoBehaviour
    {
        public String SourceImagePath;
        public String DestinationImagePath;

        public void Start()
        {
            var image = SavingFileManager.LoadPngTextureFromFile(SourceImagePath, false);
            var width = image.width;
            var height = image.height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var p = image.GetPixel(x, y);
                    var tParam = x / ((float) (width - 1));
                    p.b = tParam;
                    image.SetPixel(x,y,p);
                }
            }

            SavingFileManager.SaveTextureToPngFile(DestinationImagePath, image);
        }
    }
}
