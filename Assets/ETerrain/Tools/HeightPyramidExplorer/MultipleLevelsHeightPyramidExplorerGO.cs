using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.Pyramid.Map;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Tools.HeightPyramidExplorer
{
    public class MultipleLevelsHeightPyramidExplorerGO : MonoBehaviour
    {
        public Material ExplorerMaterial;

        private List<HeightPyramidLevel> _levels;
        private Texture _ceilTexturesArray;
        private HeightPyramidLevel _selectedLevel;

        public void Initialize(List<HeightPyramidLevel> levels, Texture ceilTexturesArray, IntVector2 slotMapSize, Dictionary<int, Vector2> ringsUvRange,
            Dictionary<HeightPyramidLevel, float> perLevelBiggestShapeLengths)
        {
            _levels = levels;
            _selectedLevel = _levels.First();
            _ceilTexturesArray = ceilTexturesArray;
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

        public void UpdateUniforms(Vector3 travellerPosition, Dictionary<HeightPyramidLevel, Vector2> pyramidCenterWorldSpacePerLevel)
        {
            ExplorerMaterial.SetVector("_TravellerPosition", travellerPosition);
            foreach (var pair in pyramidCenterWorldSpacePerLevel)
            {
                ExplorerMaterial.SetVector($"_Pyramid{pair.Key.GetIndex()}WorldSpaceCenter", pair.Value);
            }
        }
    }
}