using System.Collections.Generic;
using UnityEngine;

namespace Assets.TerrainMat.Stain
{
    public class StainTerrainArray
    {
        private readonly List<ColorPack> _paletteArray;
        private readonly int[,] _paletteIndexArray;
        private readonly Vector4[,] _controlArray;

        public StainTerrainArray(List<ColorPack> paletteArray, Vector4[,] controlArray, int[,] paletteIndexArray)
        {
            _paletteArray = paletteArray;
            _controlArray = controlArray;
            _paletteIndexArray = paletteIndexArray;
        }

        public List<ColorPack> PaletteArray
        {
            get { return _paletteArray; }
        }

        public int[,] PaletteIndexArray
        {
            get { return _paletteIndexArray; }
        }

        public Vector4[,] ControlArray
        {
            get { return _controlArray; }
        }
    }
}