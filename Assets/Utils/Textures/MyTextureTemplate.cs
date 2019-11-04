using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils.Textures
{
    public class MyTextureTemplate
    {
        private Color[] _array;
        private int _height;
        private int _width;
        private TextureFormat _format;
        private bool _mipmap;
        private FilterMode _filterMode;

        public MyTextureTemplate(int height, int width, TextureFormat format, bool mipmap,
            FilterMode filterMode = FilterMode.Point)
        {
            _height = height;
            _width = width;
            _format = format;
            _mipmap = mipmap;
            _array = new Color[_height * _width];
            _filterMode = filterMode;
        }

        public void SetPixel(int x, int y, Color color)
        {
            _array[x + y * _height] = color;
        }

        public Color[] Array
        {
            get { return _array; }
        }

        public int Height
        {
            get { return _height; }
        }

        public int Width
        {
            get { return _width; }
        }

        public TextureFormat Format
        {
            get { return _format; }
        }

        public bool Mipmap
        {
            get { return _mipmap; }
        }

        public TextureWrapMode? wrapMode { get; set; }

        public FilterMode FilterMode => _filterMode;
    }

    public class MyRenderTextureTemplate
    {
        private int _height;
        private int _width;
        private RenderTextureFormat _format;
        private bool _mipmap;
        private FilterMode _filterMode;
        public Texture _sourceTexture;

        public MyRenderTextureTemplate(int height, int width, RenderTextureFormat format, bool mipmap,
            FilterMode filterMode = FilterMode.Point)
        {
            _height = height;
            _width = width;
            _format = format;
            _mipmap = mipmap;
            _filterMode = filterMode;
        }

        public int Height
        {
            get { return _height; }
        }

        public int Width
        {
            get { return _width; }
        }

        public bool Mipmap
        {
            get { return _mipmap; }
        }

        public TextureWrapMode? wrapMode { get; set; }

        public FilterMode FilterMode => _filterMode;

        public RenderTextureFormat Format => _format;

        public Texture SourceTexture
        {
            get { return _sourceTexture; }
            set { _sourceTexture = value; }
        }
    }
}