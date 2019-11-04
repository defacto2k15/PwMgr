using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GRing1SurfaceProvider : IGRingSurfaceProvider
    {
        private readonly StainTerrainServiceProxy _stainTerrainServiceProxy;
        private readonly MyRectangle _inGamePosition;

        public GRing1SurfaceProvider(StainTerrainServiceProxy stainTerrainServiceProxy,
            MyRectangle inGamePosition)
        {
            _stainTerrainServiceProxy = stainTerrainServiceProxy;
            _inGamePosition = inGamePosition;
        }

        public async Task<List<GRingSurfaceDetail>> ProvideSurfaceDetail()
        {
            var stainResourceWithCoords = await _stainTerrainServiceProxy.RetriveResource(_inGamePosition);

            var uniformsPack = new UniformsPack();
            uniformsPack.SetTexture("_PaletteTex", stainResourceWithCoords.Resource.TerrainPaletteTexture);
            uniformsPack.SetTexture("_PaletteIndexTex", stainResourceWithCoords.Resource.PaletteIndexTexture);
            uniformsPack.SetTexture("_ControlTex", stainResourceWithCoords.Resource.ControlTexture);
            uniformsPack.SetUniform("_TerrainStainUv", stainResourceWithCoords.Coords.ToVector4());

            uniformsPack.SetUniform("_TerrainTextureSize", stainResourceWithCoords.Resource.TerrainTextureSize);
            uniformsPack.SetUniform("_PaletteMaxIndex", stainResourceWithCoords.Resource.PaletteMaxIndex);

            return new List<GRingSurfaceDetail>()
            {
                new GRingSurfaceDetail()
                {
                    UniformsWithKeywords = new UniformsWithKeywords()
                    {
                        Keywords = new ShaderKeywordSet(),
                        Uniforms = uniformsPack
                    },
                    ShaderName = "Custom/Terrain/Ring1"
                }
            };
        }
    }
}