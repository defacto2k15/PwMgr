using System;
using Assets.Heightmaps.Ring1.MT;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Welding
{
    public class HeightArrayWeldingPack
    {
        private TextureWithSize _weldTexture;
        private HeightArrayWelderProxy _welderProxy;
        private CommonExecutorUTProxy _commonExecutorUtProxy;
        private bool _weldingEnabled;

        public HeightArrayWeldingPack(TextureWithSize weldTexture, HeightArrayWelderProxy welderProxy,CommonExecutorUTProxy commonExecutorUtProxy, bool weldingEnabled)
        {
            _weldTexture = weldTexture;
            _welderProxy = welderProxy;
            _commonExecutorUtProxy = commonExecutorUtProxy;
            _weldingEnabled = weldingEnabled;
        }

        public int RegisterTerrain(WeldingRegistrationTerrain terrain, Action<TerrainWeldUvs> callback)
        {
            Preconditions.Assert(_weldingEnabled," E83 Welding is not enabled");
            Action<TerrainWeldUvs> newCallback = (t) =>
            {
                _commonExecutorUtProxy.AddAction(() =>
                {

                    callback(t);

                });
            };

            return _welderProxy.RegisterTerrain(new WeldingInputTerrain()
            {
                Texture = terrain.Texture,
                DetailGlobalArea = terrain.DetailGlobalArea,
                Resolution = terrain.Resolution,
                TerrainLod = terrain.TerrainLod,
                UvCoordsPositions2D = terrain.UvCoordsPositions2D,
                WeldModificationCallback = newCallback
            });
        }

        public void RemoveTerrain(int terrainId)
        {
            Preconditions.Assert(_weldingEnabled," E83 Welding is not enabled");
            _welderProxy.RemoveTerrain(terrainId);
        }

        public TextureWithSize WeldTexture => _weldTexture;

        public bool WeldingEnabled => _weldingEnabled;
    }

    public class WeldingRegistrationTerrain
    {
        public TextureWithSize Texture;
        public MyRectangle DetailGlobalArea;
        public TerrainCardinalResolution Resolution;
        public int TerrainLod;
        public MyRectangle UvCoordsPositions2D;
    }
}