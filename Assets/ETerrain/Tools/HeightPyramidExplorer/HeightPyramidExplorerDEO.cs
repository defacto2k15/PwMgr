using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Assets.ETerrain.Pyramid;
using Assets.ETerrain.Pyramid.Map;
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
    public class HeightPyramidExplorerDEO : MonoBehaviour
    {
        [Range(0, 10)] public float NewSegmentXPosition = 0;
        [Range(0, 10)] public float NewSegmentYPosition = 0;
        [Range(0, 1)] public float NewSegmentHeight = 0.5f;

        private LegacyHeightPyramidExplorer _explorer;
        private HeightPyramidSegmentExplorerWindow _window;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            CommonExecutorUTProxy commonExecutorUtProxy = new CommonExecutorUTProxy();
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(containerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(400, 400)
                }));


            var heightPyramidMapConfiguration = new LegacyHeightPyramidMapConfiguration()
            {
                HeightPyramidLevelsCount = 5,
                SlotMapSize = new IntVector2(3,3),
                SegmentTextureResolution = new IntVector2(240, 240),
                InterSegmentMarginSize = 0.2f,
                HeightTextureFormat = RenderTextureFormat.ARGB32
            };
            
            var creator = new LegacyHeightPyramidMapCreator();
            var enties = creator.Create(textureRendererProxy, heightPyramidMapConfiguration);

            _explorer = new LegacyHeightPyramidExplorer(enties.MapManager, heightPyramidMapConfiguration.HeightPyramidLevelsCount, enties.CeilTexture, heightPyramidMapConfiguration.SegmentTextureResolution);
            _window = new HeightPyramidSegmentExplorerWindow(enties.CeilTexture);
        }

        public void FillTerrainWithNoise()
        {
                var material = new Material(Shader.Find("Custom/Tool/FillHeightTextureWithRandomValues"));
                material.SetTexture("_HeightTexture", _explorer.CeilTexture);
                Graphics.Blit(_explorer.CeilTexture, (RenderTexture)_explorer.CeilTexture, material);
        }

        public void AddNewSegment()
        {
            _explorer.AddSegmentToMap(IntVector2.FromFloat(NewSegmentXPosition,NewSegmentYPosition),NewSegmentHeight );
        }

        public void OnGUI()
        {
            _window.OnGUI();
        }

    }

    public class LegacyHeightPyramidExplorer
    {
        private readonly LegacyHeightPyramidMapManager _legacyHeightMapManager;
        private int _heightPyramidLevelsCount;
        private Texture _ceilTexture;
        private IntVector2 _segmentTextureSize;

        public LegacyHeightPyramidExplorer(LegacyHeightPyramidMapManager legacyHeightMapManager, int heightPyramidLevelsCount, Texture ceilTexture, IntVector2 segmentTextureSize)
        {
            _legacyHeightMapManager = legacyHeightMapManager;
            _heightPyramidLevelsCount = heightPyramidLevelsCount;
            _ceilTexture = ceilTexture;
            _segmentTextureSize = segmentTextureSize;
        }

        public void AddSegmentToMap(IntVector2 segmentAlignedPosition, float height)
        {
            var segmentTexture = new Texture2D(_segmentTextureSize.X, _segmentTextureSize.Y, TextureFormat.RGBA32, false);
            for (int x = 0; x < _segmentTextureSize.X; x++)
            {
                for (int y = 0; y < _segmentTextureSize.Y; y++)
                {
                    segmentTexture.SetPixel(x,y,new Color(height,height,height));
                }
            }
            segmentTexture.Apply();

            _legacyHeightMapManager.AddSegment(segmentTexture, HeightPyramidLevel.Top, segmentAlignedPosition);
        }

        public Texture CeilTexture => _ceilTexture;
        public int LevelsCount => _heightPyramidLevelsCount;
    }

    public class HeightPyramidSegmentExplorerWindow
    {
        private readonly Texture _ceilTexture;
        private Material _heightTextureDrawingMaterial;
        private float _levelSelectSliderValue = 0;

        public HeightPyramidSegmentExplorerWindow(Texture ceilTexture)
        {
            _ceilTexture = ceilTexture;
            _heightTextureDrawingMaterial = new Material(Shader.Find("Custom/Tool/DrawHeightTextureMipMaps"));
        }

        public void OnGUI()
        {
            _heightTextureDrawingMaterial.SetTexture("_HeightTextureArray", _ceilTexture);
            _heightTextureDrawingMaterial.SetInt("_HeightTextureSelectedLevel", (int) _levelSelectSliderValue);

            _heightTextureDrawingMaterial.SetTexture("_HeightTexture", _ceilTexture);
            _heightTextureDrawingMaterial.SetInt("_HeightTextureMipLevel", (int) _levelSelectSliderValue);
            Graphics.DrawTexture(new Rect(30, 30, 240, 240),
                _ceilTexture, _heightTextureDrawingMaterial);
        }
    }

    public class HeightPyramidExplorer2
    {
        private readonly Dictionary<HeightPyramidLevel, Texture> _ceilTextures;
        private readonly Dictionary<HeightPyramidLevel, HeightPyramidSegmentExplorerWindow> _explorerWindows;
        private HeightPyramidLevel _currentLevel;

        public HeightPyramidExplorer2(Dictionary<HeightPyramidLevel, Texture> ceilTextures)
        {
            _ceilTextures = ceilTextures;
            _explorerWindows = _ceilTextures.ToDictionary(c => c.Key, c => new HeightPyramidSegmentExplorerWindow(c.Value));
            _currentLevel = _ceilTextures.Keys.First();
        }

        public void OnGUI()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                MoveCurrentSegmentLevel(1);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                MoveCurrentSegmentLevel(-1);
            }
            _explorerWindows[_currentLevel].OnGUI();
        }

        private void MoveCurrentSegmentLevel(int delta)
        {
            var currentIndex = _currentLevel.GetIndex();
            var levelCount = _ceilTextures.Count;
            var nextIndex = (currentIndex + delta + levelCount) % levelCount;
            var nextLevel = _ceilTextures.Keys.First(c => c.GetIndex() == nextIndex);
            _currentLevel = nextLevel;
            Debug.Log("M721: Next selected pyramid explorer level is "+_currentLevel);
        }
    }

}

