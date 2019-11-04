using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class TessalationRequirementTexture
    {
        private readonly Texture2D _tex;

        public TessalationRequirementTexture(Texture2D tex)
        {
            _tex = tex;
        }

        public Texture2D Texture
        {
            get { return _tex; }
        }
    }
}