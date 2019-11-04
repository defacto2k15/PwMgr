using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Roads.Engraving;
using Assets.Roads.Files;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Roads.TerrainFeature
{
    public class RoadEngravingTerrainFeatureApplier : ITerrainFeatureApplier
    {
        private PathProximityTextureDbProxy _pathProximityDb;
        private readonly RoadEngraver _roadEngraver;
        private readonly RoadEngravingTerrainFeatureApplierConfiguration _configuration;

        public RoadEngravingTerrainFeatureApplier(PathProximityTextureDbProxy pathProximityDb,
            RoadEngraver roadEngraver,
            RoadEngravingTerrainFeatureApplierConfiguration configuration)
        {
            _roadEngraver = roadEngraver;
            _configuration = configuration;
            _pathProximityDb = pathProximityDb;
        }

        public async Task<TextureWithCoords> ApplyFeatureAsync(
            TextureWithCoords textureWithCoords,
            TerrainCardinalResolution resolution,
            bool canMultistep)
        {
            Preconditions.Assert(resolution == TerrainCardinalResolution.MAX_RESOLUTION,
                "roads are engraved only in max resolution");

            var terrainTexture = textureWithCoords.Texture;
            var terrainCoords = textureWithCoords.Coords;

            var proximityTexture = await _pathProximityDb.Query(terrainCoords);

            var textureWithEngraving =
                await _roadEngraver.EngraveRoads(terrainCoords, _configuration.TerrainTextureSize,
                    proximityTexture, terrainTexture);

            return new TextureWithCoords(new TextureWithSize()
            {
                Texture = textureWithEngraving,
                Size = _configuration.TerrainTextureSize
            }, terrainCoords);
        }
    }

    public class RoadEngravingTerrainFeatureApplierConfiguration
    {
        public IntVector2 TerrainTextureSize = new IntVector2(241, 241);
    }
}