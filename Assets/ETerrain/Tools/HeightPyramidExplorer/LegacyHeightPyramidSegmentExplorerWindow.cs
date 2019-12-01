using System;
using System.Runtime.CompilerServices;
using System.Text;
using Assets.ETerrain.Pyramid;
using Assets.Gui;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.ETerrain.Tools.HeightPyramidExplorer
{
    public class LegacyHeightPyramidSegmentExplorerWindow
    {
        private readonly Texture _floorTexture;
        private Material _heightTextureDrawingMaterial;
        private float _levelSelectSliderValue = 0;

        public LegacyHeightPyramidSegmentExplorerWindow(Texture floorTexture)
        {
            _floorTexture = floorTexture;
            _heightTextureDrawingMaterial = new Material(Shader.Find("Custom/Tool/DrawHeightTextureMipMaps"));
        }

        public void OnGUI()
        {
            _heightTextureDrawingMaterial.SetTexture("_HeightTextureArray", _floorTexture);
            _heightTextureDrawingMaterial.SetInt("_HeightTextureSelectedLevel", (int) _levelSelectSliderValue);

            _heightTextureDrawingMaterial.SetTexture("_HeightTexture", _floorTexture);
            _heightTextureDrawingMaterial.SetInt("_HeightTextureMipLevel", (int) _levelSelectSliderValue);
            Graphics.DrawTexture(new Rect(30, 30, 240, 240),
                _floorTexture, _heightTextureDrawingMaterial);
        }
    }
}

