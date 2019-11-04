using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using ImageProcessor;
using ImageProcessor.Imaging;
using UnityEngine;
using Color = System.Drawing.Color;

namespace Assets.NPRResources.TonalArtMap
{
    public class TamIdPackGenerator
    {
        public TamIdSoleImagesPack GenerateTamPack( TamIdPackGenerationConfiguration configuration, bool generateDebugPlates, ComputeShaderContainerGameObject shaderContainerGameObject)
        {
            var tones = configuration.Tones;
            var levels = configuration.Levels;
            var templateGenerator = new TAMTemplateGenerator(
                new PoissonTAMImageDiagramGenerator(
                    new TAMPoissonDiskSampler(),
                    new StrokesGenerator(configuration.StrokesGeneratorConfiguration),
                    configuration.PoissonTamImageDiagramGeneratorConfiguration)
            );

            var template = templateGenerator.Generate(new TAMTemplateSpecification()
            {
                Tones = tones,
                MipmapLevels = levels
            });

            var margin = configuration.Margin;
            var smallestLevelSoleImageResolution = configuration.SmallestLevelSoleImageResolution;

            Debug.Log("XXXy: "+configuration.StrokeImagePath);
            var a1 = Image.FromFile(configuration.StrokeImagePath);
            var a2 =
                Image.FromFile(configuration.BlankImagePath);
            var renderer = new TamIdDeckRenderer(
                Image.FromFile(configuration.StrokeImagePath),
                Image.FromFile(configuration.BlankImagePath),
                new TAMDeckRendererConfiguration()
                {
                    UseSmoothAlpha = configuration.UseSmoothAlpha,
                    UseDithering = configuration.UseDithering,
                    SoleImagesResolutionPerLevel = levels
                        .Select((level, i) => new {level, i})
                        .ToDictionary(c => c.level, c => c.i)
                        .ToDictionary(pair => pair.Key, pair => (smallestLevelSoleImageResolution * Mathf.Pow(2, pair.Value)).ToIntVector()),
                    Margin = margin,
                    StrokeHeightMultiplierPerLevel = levels
                        .Select((level, i) => new {level, i})
                        .ToDictionary(c => c.level, c => c.i)
                        .ToDictionary(pair => pair.Key, pair => configuration.StrokeHeightMultiplierForZeroLevel/Mathf.Pow(2, pair.Value))
                }, configuration.LayersCount);
            var deck = renderer.Render(template);

            var wrapper = new TAMMarginsWrapper(new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(shaderContainerGameObject),
                new TextureRendererServiceConfiguration()
                {
                    StepSize = configuration.RendererOneStepSize
                })), new TAMMarginsWrapperConfiguration()
            {
                Margin = margin,
            });

            var soleImagesPack = new TamIdSoleImagesPack(deck.Columns.ToDictionary(
                c => c.Key,
                c => c.Value.ToDictionary(
                    k => k.Key,
                    k =>
                    {
                        var  x1 = k.Value.Select(r => wrapper.WrapTexture(TAMUtils.ImageToTexture2D(r))).ToList();
                        return x1;

                    })));

            deck.DisposeImages();

            return soleImagesPack;
        }
    }

    public class TamIdSoleImagesPack
    {
        private readonly Dictionary<TAMTone, Dictionary<TAMMipmapLevel, List<Texture2D>>> _columns;

        public TamIdSoleImagesPack(Dictionary<TAMTone, Dictionary<TAMMipmapLevel, List<Texture2D>>> columns)
        {
            _columns = columns;
        }

        public Dictionary<TAMTone, Dictionary<TAMMipmapLevel, List<Texture2D>>> Columns => _columns;
    }

    public class TamIdDeckRenderer
    {
        private System.Drawing.Image _strokeImage;
        private System.Drawing.Image _blankImage;
        private readonly  TAMDeckRendererConfiguration _configuration;
        private int _layersCount;

        public TamIdDeckRenderer(Image strokeImage, Image blankImage, TAMDeckRendererConfiguration configuration, int layersCount)
        {
            _configuration = configuration;
            _layersCount = layersCount;
            _strokeImage = strokeImage;
            _blankImage = blankImage;
        }

        public TamIdDeck Render(TAMTemplate template)
        {
            return new TamIdDeck(template.Columns.ToDictionary(c => c.Key, c => c.Value.ToDictionary(k => k.Key, k => CreateSoleLayerImages(k.Value, k.Key))));
        }

        private List<Image> CreateSoleLayerImages(TAMImageDiagram diagram, TAMMipmapLevel level)
        {
            var margin = _configuration.Margin;
            var soleImageResolution = _configuration.SoleImagesResolutionPerLevel[level];
            var soleImageResolutionWithMargins = (soleImageResolution * (1 + margin * 2)).ToIntVector2();
            var marginLength = (soleImageResolutionWithMargins - soleImageResolution)/2;

            var outImages = Enumerable.Range(0, _layersCount)
                .Select(c => new Bitmap((int) soleImageResolutionWithMargins.X, (int) soleImageResolutionWithMargins.Y, PixelFormat.Format32bppPArgb))
                .ToList();

            var occupancyArray = new int[(int) soleImageResolutionWithMargins.X, (int) soleImageResolutionWithMargins.Y];

            //Debug.Log($"OutiMages: {outImages[0].Size} || SoleImageResolution {soleImageResolution} Sole with margins {soleImageResolutionWithMargins}");
            foreach (var stroke in diagram.Strokes)
            {
                using (ImageFactory strokeFactory = new ImageFactory(true))
                {
                    var strokeSizeInPixels = VectorUtils.MemberwiseMultiply(
                        new Vector2(stroke.Length, stroke.Height * _configuration.StrokeHeightMultiplierPerLevel[level]), soleImageResolution.ToFloatVec()).ToIntVector2();
                    var rotation = Mathf.Rad2Deg * stroke.Rotation;

                    var rotatedStrokeImage = strokeFactory.Load(_strokeImage)
                        .Resize(new ResizeLayer(new Size(strokeSizeInPixels.X, strokeSizeInPixels.Y), ResizeMode.Stretch))
                        .Rotate(rotation).Image;
                    var imageWithId = AddIdToStrokeImage(rotatedStrokeImage, (uint) stroke.Id);

                    var position = VectorUtils.MemberwiseMultiply(UvToMarginUv(stroke.Center), soleImageResolutionWithMargins.ToFloatVec()).ToIntVector2();
                    position -= new IntVector2(rotatedStrokeImage.Width / 2, rotatedStrokeImage.Height / 2);

                    for (int x = 0; x < imageWithId.Size.Width; x++)
                    {
                        for (int y = 0; y < imageWithId.Size.Height; y++)
                        {
                            var inImagePosition = position + new IntVector2(x, y);

                            uint offsetBitX = 0;
                            uint offsetBitY = 0;
                            if (inImagePosition.X - marginLength.X/2 >= soleImageResolution.X || inImagePosition.X - marginLength.X*1.5 <0 )
                            {
                                offsetBitX = 1;
                            }
                            if (inImagePosition.Y - marginLength.Y/2 >= soleImageResolution.Y || inImagePosition.Y - marginLength.Y*1.5 <0 )
                            {
                                offsetBitY = 1;
                            }

                            inImagePosition.X = inImagePosition.X % soleImageResolutionWithMargins.X; //probably not needed
                            inImagePosition.Y = inImagePosition.Y % soleImageResolutionWithMargins.Y;

                            var strokePixel = imageWithId.GetPixel(x, y);

                            byte newGColor = (byte) ((strokePixel.G | (offsetBitX << 6)) | offsetBitY << 7);
                            strokePixel = Color.FromArgb(strokePixel.A, strokePixel.R, newGColor, strokePixel.B);

                            if (strokePixel.A > 0)
                            {
                                var layerIndex = 0;
                                var occupancy = occupancyArray[inImagePosition.X, inImagePosition.Y];
                                if (occupancy != 0)
                                {
                                    layerIndex = (occupancy ) % _layersCount;
                                }
                                occupancyArray[inImagePosition.X, inImagePosition.Y]++;

                                if (!_configuration.UseSmoothAlpha)
                                {
                                    var a = strokePixel.A;
                                    if (a > 0)
                                    {
                                        a = 255;
                                    }

                                    strokePixel = Color.FromArgb(a, strokePixel.R, strokePixel.G, strokePixel.B);
                                }

                                if (!_configuration.UseDithering)
                                {
                                    outImages[layerIndex].SetPixel(inImagePosition.X, inImagePosition.Y, strokePixel);
                                }
                                else
                                {
                                    var oldPixel = outImages[layerIndex].GetPixel(inImagePosition.X, inImagePosition.Y);
                                    if (_configuration.UseSmoothAlpha)
                                    {
                                        var oldAlpha = oldPixel.A;
                                        var maxAlpha = Mathf.Max(oldAlpha, strokePixel.A);
                                        strokePixel = Color.FromArgb(maxAlpha, strokePixel.R, strokePixel.G, strokePixel.B);

                                        if (layerIndex != 0)
                                        {
                                            var layer0Pixel = outImages[0].GetPixel(inImagePosition.X, inImagePosition.Y);
                                            var layer0OldAlpha = layer0Pixel.A;
                                            if (layer0OldAlpha < strokePixel.A)
                                            {
                                                var newLayer0Pixel = Color.FromArgb(strokePixel.A, layer0Pixel.R, layer0Pixel.G, layer0Pixel.B);
                                                outImages[0].SetPixel(inImagePosition.X, inImagePosition.Y, newLayer0Pixel);
                                            }
                                        }
                                    }

                                    var ditheringModuler = Mathf.CeilToInt((float) occupancy / _layersCount ) + 1;
                                    bool ditherPixelActive = (inImagePosition.X + inImagePosition.Y) % ditheringModuler == 0;

                                    if (ditherPixelActive)
                                    {
                                        outImages[layerIndex].SetPixel(inImagePosition.X, inImagePosition.Y, strokePixel);
                                    }
                                    else
                                    {
                                        var updatedOldPixel = Color.FromArgb(strokePixel.A, oldPixel.R, oldPixel.G, oldPixel.B);
                                        outImages[layerIndex].SetPixel(inImagePosition.X, inImagePosition.Y, updatedOldPixel);
                                    }
                                }
                            }
                        }
                    }

                }
            }

            return outImages.Cast<Image>().ToList();
        }

        private Bitmap AddIdToStrokeImage(Image image, uint id)
        {
            var bitmap = new Bitmap(image);

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    Color newPixel = Color.FromArgb(0, 0, 0, 0);
                    if (pixel.A > 0)
                    {
                        byte r = (byte) (id % 256);
                        byte g = (byte) (Mathf.RoundToInt(((float) id) / 256) % 64);
                        // two highest g bits are for x and y offset
                        newPixel = Color.FromArgb(pixel.A, r, g, pixel.B);
                    }

                    bitmap.SetPixel(x, y, newPixel);
                }
            }

            return bitmap;
        }


        private Vector2 UvToMarginUv(Vector2 position)
        {
            var margin = _configuration.Margin;
            return new Vector2(margin, margin) + position * (1-margin*2);
        }
    }


    public class TamIdDeck
    {
        private readonly Dictionary<TAMTone, Dictionary<TAMMipmapLevel, List<Image>>> _columns;

        public TamIdDeck(Dictionary<TAMTone, Dictionary<TAMMipmapLevel, List<Image>>> columns)
        {
            _columns = columns;
        }

        public Dictionary<TAMTone, Dictionary<TAMMipmapLevel, List<Image>>> Columns => _columns;

        public void DisposeImages()
        {
            foreach (var image in Columns.Values.SelectMany(c => c.Values).SelectMany(c => c))
            {
                image.Dispose();
            }
        }
    }

    public class TamIdPackGenerationConfiguration : TAMPackGenerationConfiguration
    {
        public int LayersCount;
        public bool UseDithering;
        public bool UseSmoothAlpha;

        public static TamIdPackGenerationConfiguration GetDefaultTamIdConfiguration(List<TAMTone> tones, List<TAMMipmapLevel> levels,
            float exclusionZoneMultiplier, int layersCount, string strokeImagePath, string blankImagePath, bool useDithering=true)
        {
            var defaultTamConfiguration = GetDefaultConfiguration(tones, levels, exclusionZoneMultiplier, strokeImagePath, blankImagePath);

            return new TamIdPackGenerationConfiguration()
            {
                PoissonTamImageDiagramGeneratorConfiguration = defaultTamConfiguration.PoissonTamImageDiagramGeneratorConfiguration
                , StrokesGeneratorConfiguration = defaultTamConfiguration.StrokesGeneratorConfiguration
                , BlankImagePath = defaultTamConfiguration.BlankImagePath
                ,StrokeImagePath = defaultTamConfiguration.StrokeImagePath
                , Margin = defaultTamConfiguration.Margin
                , SmallestLevelSoleImageResolution = defaultTamConfiguration.SmallestLevelSoleImageResolution
                , RendererOneStepSize = defaultTamConfiguration.RendererOneStepSize
                , Tones = tones
                , Levels = levels
                , StrokeHeightMultiplierForZeroLevel = defaultTamConfiguration.StrokeHeightMultiplierForZeroLevel*2.5f
                , LayersCount = layersCount
                ,UseDithering=useDithering
                ,UseSmoothAlpha = false
            };
        }
    }

}