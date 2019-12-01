using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.ETerrainIntegration.deos;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Tools.HeightPyramidExplorer
{
    public class MultipleLevelsHeightPyramidExplorerGO : MonoBehaviour
    {
        public Material ExplorerMaterial;

        private List<HeightPyramidLevel> _levels;
        private Texture _ceilTexturesArray;
        private IntVector2 _slotMapSize;
        private HeightPyramidLevel _selectedLevel;
        private ComputeBuffer _fillingStateBuffer;

        public void Initialize(List<HeightPyramidLevel> levels, Texture ceilTexturesArray, IntVector2 slotMapSize, Dictionary<int, Vector2> ringsUvRange,
            Dictionary<HeightPyramidLevel, float> perLevelBiggestShapeLengths)
        {
            _levels = levels;
            _selectedLevel = _levels.First();
            _ceilTexturesArray = ceilTexturesArray;
            _slotMapSize = slotMapSize;
            ExplorerMaterial.SetTexture("_CeilTexturesArray", ceilTexturesArray);
            ExplorerMaterial.SetVector("_SlotMapSize", new Vector4(slotMapSize.X, slotMapSize.Y, 0,0));

            var packedRingRanges = new Vector4();
            for (int i = 0; i < ringsUvRange.Count; i++)
            {
                packedRingRanges[i] = ringsUvRange[i].y;
            }
            ExplorerMaterial.SetVector($"_RingsUvRange", packedRingRanges);

            var perLevelCeilTextureWorldSpaceSize = perLevelBiggestShapeLengths.ToDictionary(c => c.Key, c => c.Value * slotMapSize.X); //TODO what whith Y
            var packedSizes = new Vector4();
            foreach (var pair in perLevelCeilTextureWorldSpaceSize)
            {
                packedSizes[pair.Key.GetIndex()] = pair.Value;
            }

            ExplorerMaterial.SetVector("_PerLevelCeilTextureWorldSpaceSizes", packedSizes);

            _fillingStateBuffer = new ComputeBuffer(slotMapSize.X*slotMapSize.Y*_levels.Count, sizeof(int)*3);
            ExplorerMaterial.SetBuffer("_FillingStateBuffer", _fillingStateBuffer);
        }

        public void OnGUI()
        {
            if (_ceilTexturesArray != null)
            {
                if (Input.GetKeyDown(KeyCode.Comma))
                {
                    MoveCurrentSegmentLevel(1);
                }

                if (Input.GetKeyDown(KeyCode.Period))
                {
                    MoveCurrentSegmentLevel(-1);
                }

                DrawExplorerWindow();
            }
        }

        private void DrawExplorerWindow()
        {
            ExplorerMaterial.SetFloat("_SelectedLevelIndex", _selectedLevel.GetIndex());
            var size = Mathf.Min(Screen.width * 0.5f, Screen.height * 0.5f);
            Graphics.DrawTexture(new Rect(Screen.width-size, 0,  size, size), _ceilTexturesArray, ExplorerMaterial);
        }

        private void MoveCurrentSegmentLevel(int delta)
        {
            var newLevelIndex = (_levels.Count + _levels.IndexOf(_selectedLevel) + delta) % _levels.Count;
            _selectedLevel = _levels[newLevelIndex];
            Debug.Log("HeightPyramidExplorer: selected level "+_selectedLevel);
        }

        public void UpdateTravellingUniforms(Vector3 travellerPosition, Dictionary<HeightPyramidLevel, Vector2> pyramidCenterWorldSpacePerLevel)
        {
            ExplorerMaterial.SetVector("_TravellerPosition", travellerPosition);
            foreach (var pair in pyramidCenterWorldSpacePerLevel)
            {
                ExplorerMaterial.SetVector($"_Pyramid{pair.Key.GetIndex()}WorldSpaceCenter", pair.Value);
            }
        }

        public void UpdateHeightmapSegmentFillingState(Dictionary<HeightPyramidLevel, Dictionary<IntVector2,  SegmentGenerationProcessTokenWithFillingNecessity>> tokens)
        {
            var fillingStateArray = new GpuSingleSegmentState[_slotMapSize.X * _slotMapSize.Y * _levels.Count];

            foreach (var pair in tokens.OrderBy(c => c.Key.GetIndex()))
            {
                FillStateArrayFromSoleLevelData(pair.Value, fillingStateArray, pair.Key.GetIndex()*_slotMapSize.X*_slotMapSize.Y);
            }

            _fillingStateBuffer.SetData(fillingStateArray);
        }

        private void FillStateArrayFromSoleLevelData(Dictionary<IntVector2,   SegmentGenerationProcessTokenWithFillingNecessity> tokens, GpuSingleSegmentState[] fillingStateArray, int fillingStateArrayOffset)
        {
            var tokensArray = new   SegmentGenerationProcessTokenWithFillingNecessity[_slotMapSize.X * _slotMapSize.Y];
            foreach (var pair in tokens)
            {
                var moddedCoords = new IntVector2((pair.Key.X + _slotMapSize.X * 32) % _slotMapSize.X, (pair.Key.Y + _slotMapSize.Y * 32) % _slotMapSize.Y);
                tokensArray[moddedCoords.X + moddedCoords.Y * _slotMapSize.X] = pair.Value;
            }

            for (int x = 0; x < _slotMapSize.X; x++)
            {
                for (int y = 0; y < _slotMapSize.Y; y++)
                {
                    var index = x + y * _slotMapSize.Y;
                    var token = tokensArray[index];
                    var state = new GpuSingleSegmentState()
                    {
                        IsProcessOnGoing = 0,
                        CurrentSituationIndex = 0,
                        RequiredSituationIndex = 0
                    };
                    if (token != null)
                    {
                        if (token.Token.ProcessIsOngoing)
                        {
                            state.IsProcessOnGoing = 1;
                        }

                        state.CurrentSituationIndex = (int) token.Token.CurrentSituation;
                        state.RequiredSituationIndex = (int) token.Token.RequiredSituation;
                    }

                    fillingStateArray[index+fillingStateArrayOffset] = state;
                }
            }
        }


        private struct GpuSingleSegmentState
        {
            public int IsProcessOnGoing;
            public int RequiredSituationIndex;
            public int CurrentSituationIndex;
        }
    }

}