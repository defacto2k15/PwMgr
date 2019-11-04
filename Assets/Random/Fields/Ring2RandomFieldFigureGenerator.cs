using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Random.Fields
{
    public class Ring2RandomFieldFigureGenerator
    {
        private string _shaderName = "Custom/Misc/RandomImageGenerator";
        private TextureRenderer _textureRenderer;
        private Ring2RandomFieldFigureGeneratorConfiguration _configuration;
        private Texture _inputBlankTexture;

        public Ring2RandomFieldFigureGenerator(TextureRenderer textureRenderer,
            Ring2RandomFieldFigureGeneratorConfiguration configuration)
        {
            _textureRenderer = textureRenderer;
            _configuration = configuration;
            Initialize();
        }

        public void Initialize()
        {
            _inputBlankTexture = new Texture2D(4, 4, TextureFormat.ARGB32, false);
        }

        public IntensityFieldFigure Generate(RandomFieldNature nature, float seed,
            MyRectangle segmentCoords) //todo use nature
        {
            UniformsPack uniforms = new UniformsPack();
            uniforms.SetUniform("_Seed", seed);
            uniforms.SetUniform("_Coords",
                new Vector4(segmentCoords.X, segmentCoords.Y, segmentCoords.Width, segmentCoords.Height));

            Vector2 imageSize = new Vector2(
                (int) Mathf.Round(_configuration.PixelsPerUnit.x * segmentCoords.Width),
                (int) Mathf.Round(_configuration.PixelsPerUnit.y * segmentCoords.Height)
            );

            RenderTextureInfo renderTextureInfo = new RenderTextureInfo(
                (int) imageSize.x,
                (int) imageSize.y,
                RenderTextureFormat.ARGB32
            );

            ConventionalTextureInfo outTextureInfo = new ConventionalTextureInfo(
                (int) imageSize.x,
                (int) imageSize.y,
                TextureFormat.ARGB32
            );

            Texture2D outTexture = _textureRenderer.RenderTexture(_shaderName, _inputBlankTexture, uniforms,
                renderTextureInfo, outTextureInfo);

            var toReturn = CopyToRandomFieldFigure(outTexture);
            if (DebugLastGeneratedTexture != null)
            {
                GameObject.Destroy(DebugLastGeneratedTexture);
            }
            DebugLastGeneratedTexture = outTexture;
            //GameObject.Destroy(outTexture);

            return toReturn;
        }

        public static Texture2D DebugLastGeneratedTexture;

        private IntensityFieldFigure CopyToRandomFieldFigure(Texture2D outTexture)
        {
            var randomFieldFigure = new IntensityFieldFigure(outTexture.width, outTexture.height);
            for (int x = 0; x < outTexture.width; x++)
            {
                for (int y = 0; y < outTexture.height; y++)
                {
                    randomFieldFigure.SetPixel(x, y, outTexture.GetPixel(x, y).r);
                }
            }
            return randomFieldFigure;
        }
    }

    public class Ring2RandomFieldFigureGeneratorConfiguration
    {
        public Vector2 PixelsPerUnit;
    }
}