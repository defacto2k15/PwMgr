using UnityEngine;

namespace Assets.Ring2.PatchTemplateToPatch
{
    public class Ring2SlicePalette
    {
        private readonly Color[] _palette;

        public Ring2SlicePalette(Color[] palette)
        {
            _palette = palette;
        }

        public Color[] Palette
        {
            get { return _palette; }
        }
    }
}