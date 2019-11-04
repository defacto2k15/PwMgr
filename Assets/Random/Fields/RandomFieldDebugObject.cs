using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Random.Fields
{
    public class RandomFieldDebugObject : MonoBehaviour
    {
        public Texture2D MyTexture;

        public void Start()
        {
            var provider = new Ring2RandomFieldFigureGenerator(
                new TextureRenderer(), new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(80, 80)
                });
            var figure = provider.Generate(RandomFieldNature.FractalSimpleValueNoise3, 0,
                new MyRectangle(0, 0, 10, 10));

            var newTexture = new Texture2D(figure.Width, figure.Height, TextureFormat.ARGB32, false);
            for (int x = 0; x < figure.Width; x++)
            {
                for (int y = 0; y < figure.Height; y++)
                {
                    newTexture.SetPixel(x, y, new Color(figure.GetPixel(x, y), 0, 0));
                }
            }
            newTexture.Apply();
            MyTexture = newTexture;
        }
    }
}