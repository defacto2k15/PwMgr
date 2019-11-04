using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class HeightmapBundle
    {
        private readonly List<OneLevelHeightmapPack> _packList;
        private int _baseWidth;

        public HeightmapBundle(List<OneLevelHeightmapPack> packList, int baseWidth)
        {
            _packList = packList;
            _baseWidth = baseWidth;
        }

        public Texture2D GetHeightmapTextureForLod(int lodLevel)
        {
            var expectedWidth = GetExpectedWidth(lodLevel);
            return GetPackOfGivenWidth(expectedWidth).HeightmapTexture;
        }

        private int GetExpectedWidth(int lodLevel)
        {
            int expectedWidth = _baseWidth / (int) Math.Pow(2, MyConstants.MAXIMUM_LOD_LEVEL - lodLevel);
            return expectedWidth;
        }

        public Texture2D GetNormalTextureForLod(int lodLevel)
        {
            var expectedWidth = GetExpectedWidth(lodLevel);
            return GetPackOfGivenWidth(expectedWidth).NormalTexture;
        }

        private OneLevelHeightmapPack GetPackOfGivenWidth(int expectedWidth)
        {
            var orderedPacks = _packList.OrderBy(c => c.Width).ToList();
            foreach (var pack in orderedPacks)
            {
                if (pack.Width == expectedWidth)
                {
                    return pack;
                }
                if (expectedWidth < pack.Width)
                {
                    return pack;
                }
            }
            return orderedPacks.Last();
        }

        public List<OneLevelHeightmapPack> PackList
        {
            get { return _packList; }
        }
    }

    public class OneLevelHeightmapPack
    {
        private HeightmapArray _heightmapArray;
        private Texture2D _heightmapTexture;
        private Texture2D _normalTexture;

        public OneLevelHeightmapPack(HeightmapArray heightmapArray, Texture2D heightmapTexture, Texture2D normalTexture)
        {
            this._heightmapArray = heightmapArray;
            _heightmapTexture = heightmapTexture;
            _normalTexture = normalTexture;
        }

        public HeightmapArray HeightmapArray
        {
            get { return _heightmapArray; }
        }

        public Texture2D HeightmapTexture
        {
            get { return _heightmapTexture; }
        }

        public Texture2D NormalTexture
        {
            get { return _normalTexture; }
        }

        public int Width
        {
            get { return _heightmapArray.Width; }
        }
    }
}