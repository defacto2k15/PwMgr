using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Random.Fields;
using UnityEngine;

namespace Assets.ETerrain.Tools
{
    public class MipMappingTestDEO : MonoBehaviour
    {
        public void Start()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RFloat, true);
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    tex.SetPixel(x,y, new Color(x/32f,0,0));
                }
            }
            tex.Apply(true);
            var infos = LoadMipmapInfos(tex);

            PrintHeight(0, 0, tex);
            PrintHeight(5, 0, tex);
            PrintHeight(10, 0, tex);
            PrintHeight(31, 0, tex);
            PrintHeight(10, 10, tex);

            PrintHeightWithLevel(0,0, 1, infos);
            PrintHeightWithLevel(0,0, 2, infos);

        }

        private void PrintHeight(int x, int y, Texture2D tex)
        {
            Debug.Log($"Li: {x}:{y} = {tex.GetPixel(x,y).r}");
        }

        private void PrintHeightWithLevel(int x, int y, int mipLevel, List<IntensityFieldFigure> figures)
        {
            Debug.Log($"Li: {x}:{y} mip{mipLevel} = {figures[mipLevel].GetPixel(x,y)}");
        }

        public List<IntensityFieldFigure> LoadMipmapInfos(Texture2D tex)
        {
            return new List<IntensityFieldFigure>()
            {
                LoadSingleLevelMipmapData(tex, 0, 32),
                LoadSingleLevelMipmapData(tex, 1, 16),
                LoadSingleLevelMipmapData(tex, 2, 8),
            };
        }

        public IntensityFieldFigure LoadSingleLevelMipmapData(Texture2D tex, int level, int height)
        {
            var figure = new IntensityFieldFigure(height, height);
            var colors = tex.GetPixels(level);
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    figure.SetPixel(x,y, colors[y*height + x].r );
                }
            }
            return figure;
        }
    }
}
