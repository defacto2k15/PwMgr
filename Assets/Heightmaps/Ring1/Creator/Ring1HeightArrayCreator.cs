using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Random;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Creator
{
    class Ring1HeightArrayCreator
    {
        private const int RING1_WIDTH = 2048;
        private const int RING1_HEIGHT = 2048;
        private TextureRenderer _textureRenderer = new TextureRenderer();

        public HeightmapArray CreateRing1HeightmapArray(HeightmapArray inputHeightmap)
        {
            assertInputHeightmapHasProperSize(inputHeightmap);
            var inputTexture =
                HeightmapUtils.CreateTextureFromHeightmap(inputHeightmap); // input texture is 256x256 (5.7x5.7) p22.3m
            var conventionalRing1Texture = RenderConventionalRing1Texture(inputTexture, new UniformsPack());
            var heightmapArray = HeightmapUtils.CreateHeightmapArrayFromTexture(conventionalRing1Texture);

            DiamondSquareCreator diamondSquareCreator = new DiamondSquareCreator(new RandomProvider());
            heightmapArray = diamondSquareCreator.AddDetail(heightmapArray);
            return heightmapArray;
        }

        private void assertInputHeightmapHasProperSize(HeightmapArray inputHeightmap)
        {
            Preconditions.Assert(inputHeightmap.WorkingWidth == 256, "Working width of input heightmap must be 256");
            Preconditions.Assert(inputHeightmap.WorkingHeight == 256, "Working height of input heightmap must be 256");
        }

        private Texture2D RenderConventionalRing1Texture(Texture2D inputTexture, UniformsPack uniformsPack)
        {
            var conventionalRing1Texture = _textureRenderer.RenderTexture("Custom/Heightmap/Ring1Creator", inputTexture,
                uniformsPack,
                new RenderTextureInfo(RING1_WIDTH, RING1_HEIGHT, RenderTextureFormat.ARGB32),
                new ConventionalTextureInfo(RING1_WIDTH, RING1_HEIGHT, TextureFormat.ARGB32));
            return conventionalRing1Texture;
        }
    }
}