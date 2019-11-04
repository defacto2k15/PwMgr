using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    class RawRFloatTextureBytes
    {
        private float[] _texturePayload;
        private int _sizeLength;
        private Texture2D _texture;

        public RawRFloatTextureBytes(int sizeLength)
        {
            _texturePayload = new float[sizeLength * sizeLength];
            _sizeLength = sizeLength;
        }

        public void SetPixel(int x, int y, float value)
        {
            _texturePayload[y * _sizeLength + x] = value;
        }

        public float GetPixel(int x, int y)
        {
            return _texturePayload[y * _sizeLength + x];
        }

        public void InitializeTexture()
        {
            _texture = new Texture2D(_sizeLength, _sizeLength, TextureFormat.RFloat, false);
            _texture.filterMode = FilterMode.Point;
        }

        public void ApplyTexture()
        {
            var byteTexturePayload = new byte[_sizeLength * _sizeLength * sizeof(float)];
            Buffer.BlockCopy(_texturePayload, 0, byteTexturePayload, 0, byteTexturePayload.Length);

            _texture.LoadRawTextureData(byteTexturePayload);
            _texture.Apply();
        }

        public Texture2D GetTexture()
        {
            return _texture;
        }

        public void ClearData()
        {
            Array.Clear(_texturePayload, 0, _texturePayload.Length);
        }
    }
}