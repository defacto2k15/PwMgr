using UnityEngine;

namespace Assets.Ring2.PatchTemplateToPatch
{
    public class Ring2PatchSliceIntensityPattern
    {
        private readonly Texture _texture;

        public Ring2PatchSliceIntensityPattern(Texture texture)
        {
            _texture = texture;
        }

        public Texture Texture
        {
            get { return _texture; }
        }
    }
}