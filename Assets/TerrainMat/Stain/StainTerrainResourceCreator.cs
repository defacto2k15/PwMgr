using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.TerrainMat.Stain
{
    public class StainTerrainResourceCreatorUTProxy : BaseUTTransformProxy<StainTerrainResource,
        StainTerrainResourceTextureTemplate>
    {
        private StainTerrainResourceCreator _creator;

        public StainTerrainResourceCreatorUTProxy(StainTerrainResourceCreator creator)
        {
            _creator = creator;
        }

        protected override StainTerrainResource ExecuteOrder(StainTerrainResourceTextureTemplate template)
        {
            return _creator.GenerateTextures(template);
        }

        public Task<StainTerrainResource> GenerateResourcesAsync(StainTerrainResourceTextureTemplate template)
        {
            return BaseUtAddOrder(template);
        }
    }

    public class StainTerrainResourceCreator
    {
        public StainTerrainResource GenerateTextures(StainTerrainResourceTextureTemplate template)
        {
            var controlColorArray = template.ControlColorArray;
            var controlTexture = new Texture2D(
                controlColorArray.Width,
                controlColorArray.Height, TextureFormat.RGB24, false);
            controlTexture.SetPixels(controlColorArray.Array);
            controlTexture.Apply();

            var paletteIndexColorArray = template.PaletteIndexColorArray;
            var paletteIndexTexture = new Texture2D(
                paletteIndexColorArray.Width,
                paletteIndexColorArray.Height, TextureFormat.RHalf, false);
            paletteIndexTexture.SetPixels(paletteIndexColorArray.Array);
            paletteIndexTexture.filterMode = FilterMode.Point;
            paletteIndexTexture.Apply();


            var paletteColorArray = template.PaletteColorArray;
            Texture2D terrainPalette = new Texture2D(paletteColorArray.Width, paletteColorArray.Height,
                TextureFormat.RGB24, false);
            terrainPalette.SetPixels(paletteColorArray.Array);
            terrainPalette.filterMode = FilterMode.Point;
            terrainPalette.Apply();

            return new StainTerrainResource()
            {
                ControlTexture = controlTexture,
                PaletteIndexTexture = paletteIndexTexture,
                TerrainPaletteTexture = terrainPalette,
                PaletteMaxIndex = terrainPalette.width / 4.0f,
                TerrainTextureSize = controlTexture.width
            };
        }
    }
}