using System;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Roads.Engraving
{
    public class PathProximityTextureGenerator
    {
        private TextureConcieverUTProxy _textureConcieverUtProxy;
        private PathProximityTextureGeneratorConfiguration _configuration;

        public PathProximityTextureGenerator(TextureConcieverUTProxy textureConcieverUtProxy,
            PathProximityTextureGeneratorConfiguration configuration)
        {
            _textureConcieverUtProxy = textureConcieverUtProxy;
            _configuration = configuration;
        }


        public async Task<TextureWithSize> GeneratePathProximityTexture(PathProximityArray array)
        {
            var proximityTextureSize = array.Size;
            var textureTemplate = new MyTextureTemplate(proximityTextureSize.X, proximityTextureSize.Y,
                TextureFormat.ARGB32, false, FilterMode.Bilinear) {wrapMode = TextureWrapMode.Clamp};


            for (int x = 0; x < proximityTextureSize.X; x++)
            {
                for (int y = 0; y < proximityTextureSize.Y; y++)
                {
                    var proximityInfo = array.GetProximityInfo(x, y);
                    if (proximityInfo == null)
                    {
                        textureTemplate.SetPixel(x, y,
                            new Color(_configuration.MaximumProximity, _configuration.MaximumProximity, 0, 0));
                    }
                    else
                    {
                        var encodedProximity =
                            PathProximityUtils.EncodeProximity(proximityInfo.Distance, _configuration.MaximumProximity);
                        var centerDelta =
                            PathProximityUtils.EncodeDelta(proximityInfo.ToCenterDelta, _configuration.MaxDelta);

                        var newColor = new Color(encodedProximity.x, encodedProximity.y, centerDelta.x, centerDelta.y);
                        textureTemplate.SetPixel(x, y, newColor);
                    }
                }
            }

            var texture = await _textureConcieverUtProxy.ConcieveTextureAsync(textureTemplate);

            //SavingFileManager.SaveTextureToPngFile($@"C:\inz\debGrassIntensityTex\PATH-PROX{DateTime.Now.Ticks}.png",texture);
            return new TextureWithSize()
            {
                Texture = texture,
                Size = proximityTextureSize
            };
        }


        public class PathProximityTextureGeneratorConfiguration
        {
            public float MaximumProximity = RoadDefaultConstants.MaxProximity;
            public float MaxDelta = RoadDefaultConstants.MaxDelta;
        }
    }

    public static class PathProximityUtils
    {
        public static Vector2 EncodeProximity(float proximity, float maximumProximity)
        {
            var normalized = Mathf.Clamp01(proximity / maximumProximity);
            return new Vector2(
                Mathf.FloorToInt(normalized * 255f) / 255f,
                Mathf.Repeat(normalized * 255f, 1f));
        }

        public static Vector2 EncodeDelta(Vector2 originalDelta, float maxDelta)
        {
            var normalizedDelta = new Vector2(
                Mathf.Clamp(originalDelta.x / maxDelta, -1, 1),
                Mathf.Clamp(originalDelta.y / maxDelta, -1, 1)
            );
            return (normalizedDelta + new Vector2(1f, 1f)) / 2f; // to <0,1> range
        }
    }
}