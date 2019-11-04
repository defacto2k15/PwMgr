using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class Ring1SubmapTextures
    {
        private readonly HeightmapBundle _heightmapBundle;
        private readonly TessalationRequirementTexture _tessalationReqTexture;

        public Ring1SubmapTextures(HeightmapBundle heightmapBundle, TessalationRequirementTexture tessalationReqTexture)
        {
            _heightmapBundle = heightmapBundle;
            _tessalationReqTexture = tessalationReqTexture;
        }

        public Texture2D GetHeightmapTexture(int lod)
        {
            return _heightmapBundle.GetHeightmapTextureForLod(lod);
        }

        public TessalationRequirementTexture TessalationTexture
        {
            get { return _tessalationReqTexture; }
        }

        public Texture2D GetNormalAsTexture(int lod)
        {
            return _heightmapBundle.GetNormalTextureForLod(lod);
        }
    }
}