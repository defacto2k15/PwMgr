using System;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Random.Fields
{
    public class IntensityFieldFigure
    {
        private readonly int _width;
        private readonly int _height;
        private float[,] _field;

        public IntensityFieldFigure(int width, int height)
        {
            _width = width;
            _height = height;
            _field = new float[width, height];
        }

        public int Width => _width;

        public int Height => _height;

        public void SetPixel(int x, int y, float newValue)
        {
            _field[x, y] = newValue;
        }

        public float GetPixel(int x, int y)
        {
            return _field[x, y];
        }

        public float GetPixelWithUv(Vector2 uv)
        {
            var widthMaxIndex = _width - 1;
            var heightMaxIndex = _height - 1;

            int x1 = (int) (uv.x * widthMaxIndex);
            int x2 = (int) (uv.x * widthMaxIndex);
            float x1Weight = 1 - ((uv.x * widthMaxIndex) - x1);

            int y1 = (int) Mathf.Min(uv.y * heightMaxIndex);
            int y2 = (int) Mathf.Max(uv.y * heightMaxIndex);
            float y1Weight = 1 - ((uv.y * heightMaxIndex) - y1);

            float lerp = 0;
            lerp = Mathf.Lerp(
                Mathf.Lerp(GetPixel(x1, y1), GetPixel(x2, y1), x1Weight),
                Mathf.Lerp(GetPixel(x1, y2), GetPixel(x2, y2), x1Weight),
                y1Weight);
            return lerp;
        }
    }
}