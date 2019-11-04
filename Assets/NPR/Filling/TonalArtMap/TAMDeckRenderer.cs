using System.Drawing;
using System.IO;
using System.Linq;
using Assets.Utils;
using ImageProcessor;
using ImageProcessor.Imaging;
using UnityEngine;
using Color = System.Drawing.Color;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMDeckRenderer
    {
        private System.Drawing.Image _strokeImage;
        private System.Drawing.Image _blankImage;
        private readonly TAMDeckRendererConfiguration _configuration;

        public TAMDeckRenderer(Image strokeImage, Image blankImage, TAMDeckRendererConfiguration configuration)
        {
            _configuration = configuration;
            _strokeImage = strokeImage;
            _blankImage = blankImage;
        }

        public TonalArtMapDeck Render(TAMTemplate template)
        {
            return new TonalArtMapDeck(template.Columns.ToDictionary(c => c.Key, c => c.Value.ToDictionary(k => k.Key, k => CreateSoleImage(k.Value, k.Key))));
        }

        private Image CreateSoleImage(TAMImageDiagram diagram, TAMMipmapLevel level)
        {
            var margin = _configuration.Margin;
            var soleImageResolution = _configuration.SoleImagesResolutionPerLevel[level].ToFloatVec();
            var soleImageResolutionWithMargins = soleImageResolution * (1 + margin * 2);

            using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
            {
                var factory = imageFactory.Load(_blankImage)
                    .Resize(new Size((int) soleImageResolutionWithMargins.x, (int) soleImageResolutionWithMargins.y));
                foreach (var stroke in diagram.Strokes)
                {
                    using (ImageFactory strokeFactory = new ImageFactory(true))
                    {

                        var strokeSizeInPixels = VectorUtils.MemberwiseMultiply(
                            new Vector2(stroke.Length, stroke.Height * _configuration.StrokeHeightMultiplierPerLevel[level]), soleImageResolution).ToIntVector2();
                        var rotation = Mathf.Rad2Deg * stroke.Rotation;

                        var rotatedStrokeImageFactory = strokeFactory.Load(_strokeImage)
                            .Resize(new ResizeLayer(new Size(strokeSizeInPixels.X, strokeSizeInPixels.Y), ResizeMode.Stretch))
                            .Rotate(rotation);
                        //if (stroke.Id == 1)
                        //{
                        //    rotatedStrokeImageFactory = rotatedStrokeImageFactory.ReplaceColor(Color.Black, Color.Blue);
                        //}

                        var rotatedStrokeImage = rotatedStrokeImageFactory.Image;
                        var position = VectorUtils.MemberwiseMultiply( UvToMarginUv(stroke.Center) , soleImageResolutionWithMargins).ToIntVector2();
                        //todo przelicz tu aby brac pod uwage multiplier
                        position -= new IntVector2(rotatedStrokeImage.Width/2, rotatedStrokeImage.Height/2);

                        factory = factory.Overlay(new ImageLayer()
                        {
                            Image = rotatedStrokeImage,
                            Opacity = 100,
                            Position = new Point(position.X, position.Y)
                        });
                    }
                }

                using (Stream stream = new MemoryStream())
                {
                    factory.Save(stream);
                    return Image.FromStream(stream);
                }
            }
        }

        private Vector2 UvToMarginUv(Vector2 position)
        {
            var margin = _configuration.Margin;
            return new Vector2(margin, margin) + position * (1-margin*2);
        }
    }
}