using System.Collections.Generic;
using Assets.Random;
using UnityEngine;

namespace Assets.TerrainMat.BiomeGen
{
    public class BiomeInstancePlacementTemplate
    {
        public LeafPointsGenerator LeafPointsGenerator;
        public BiomeType Type;
        public RandomCharacteristics OccurencesPerSquareUnit;
        public RandomCharacteristics LeafPointsCount;
        public RandomCharacteristics LeafPointsDistanceCharacteristics;
    }

    public class BiomeInstanceDetailTemplate
    {
        public BiomeColorTemplate ColorTemplate;
        public BiomeControlTemplate ControlTemplate;
    }

    public class BiomeControlTemplate
    {
        public List<Vector4> BaseControlValues;
        public RandomCharacteristics DeltaControls;
    }

    public class BiomeColorTemplate
    {
        public List<ColorPack> BaseColors;
        public RandomCharacteristics DeltaCharacteristics;
    }
}