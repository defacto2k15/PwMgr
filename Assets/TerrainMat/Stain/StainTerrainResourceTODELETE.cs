using UnityEngine;

namespace Assets.TerrainMat.Stain
{
    public class StainTerrainResourceTODELETE
    {
        private readonly Texture2D _palette;
        private readonly Texture2D _control;
        private readonly Texture2D _paletteIndex;

        public StainTerrainResourceTODELETE(Texture2D palette, Texture2D control, Texture2D paletteIndex)
        {
            this._palette = palette;
            this._control = control;
            this._paletteIndex = paletteIndex;
        }

        public Texture2D Palette
        {
            get { return _palette; }
        }

        public Texture2D Control
        {
            get { return _control; }
        }

        public Texture2D PaletteIndex
        {
            get { return _paletteIndex; }
        }

        public void ConfigureMaterial(Material material)
        {
            material.SetTexture("_PaletteTex", _palette);
            material.SetTexture("_PaletteIndexTex", _paletteIndex);
            material.SetTexture("_ControlTex", _control);
            material.SetFloat("_TerrainTextureSize", _control.width);
            material.SetFloat("_PaletteMaxIndex", _palette.width / 4.0f);
        }
    }
}