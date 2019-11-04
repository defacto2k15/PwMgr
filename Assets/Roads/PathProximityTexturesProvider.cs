using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Engraving;
using Assets.Roads.Files;
using Assets.Utils;
using Assets.Utils.Spatial;
using Assets.Utils.Textures;

namespace Assets.Roads
{
    public class PathProximityTexturesProvider : IStoredPartsGenerator<TextureWithSize>
    {
        private readonly RoadDatabaseProxy _roadDatabaseProxy;
        private readonly PathProximityTextureGenerator _proximityTextureGenerator;
        private readonly PathProximityArrayGenerator _proximityArrayGenerator;
        private readonly PathProximityTextureProviderConfiguration _configuration;

        public PathProximityTexturesProvider(RoadDatabaseProxy roadDatabaseProxy,
            PathProximityTextureGenerator proximityTextureGenerator,
            PathProximityArrayGenerator proximityArrayGenerator,
            PathProximityTextureProviderConfiguration configuration)
        {
            _roadDatabaseProxy = roadDatabaseProxy;
            _proximityTextureGenerator = proximityTextureGenerator;
            _proximityArrayGenerator = proximityArrayGenerator;
            _configuration = configuration;
        }

        public async Task<CoordedPart<TextureWithSize>> GeneratePartAsync(MyRectangle queryArea)
        {
            var pathQueryRect = queryArea.EnlagreByMargins(_configuration.MaxProximity);
            var paths = await _roadDatabaseProxy.Query(pathQueryRect);
            var proximityArray = _proximityArrayGenerator.Generate(paths, queryArea);
            var proximityTexture = await _proximityTextureGenerator.GeneratePathProximityTexture(proximityArray);

            return new CoordedPart<TextureWithSize>()
            {
                Part = proximityTexture,
                Coords = queryArea
            };
        }
    }

    public class PathProximityTextureProviderConfiguration
    {
        public float MaxProximity = RoadDefaultConstants.MaxProximity;
    }
}