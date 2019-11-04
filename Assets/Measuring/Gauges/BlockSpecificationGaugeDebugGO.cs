using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring.DebugIllustration;
using Assets.Utils;
using UnityEngine;

namespace Assets.Measuring.Gauges
{
    public class BlockSpecificationGaugeDebugGO : MonoBehaviour
    {
        private RunOnceBox _once;
        public void Start()
        {
            _once = new RunOnceBox(() =>
            {
                var screenSize = new IntVector2(Screen.width, Screen.height);
                var blockSize = new IntVector2(16,16);
                var blockCount = new IntVector2(Mathf.CeilToInt(screenSize.X / (float)blockSize.X), Mathf.CeilToInt(screenSize.Y/(float)blockSize.Y));

                var divisionSettings = new GridDivisionSettings()
                {
                    BlockCount = blockCount,
                    BlockSize = blockSize,
                    ScreenSize = screenSize
                };

                FindObjectOfType<LineMeasuringPpDirectorOc>().RequireScreenshotsSet((screenshotsSet) =>
                {
                    var gauge = new BlockSpecificationGauge(divisionSettings);
                    var result = gauge.TakeMeasurement(screenshotsSet);

                    var ppDirector = FindObjectOfType<DebugIllustrationPPDirectorOC>();
                    ppDirector.ShowIllustrations(screenshotsSet.HatchMainTexture.ToTexture2D(), result.GenerateIllustration());
                    File.WriteAllText(@"C:\mgr\tmp\blockFilling.json", JsonUtility.ToJson(result.GeneratePoco()));
                });

            }, 4);
        }

        public void Update()
        {
            _once.Update();
        }
    }

    public class BlockSpecificationGauge : IGauge
    {
        private GridDivisionSettings _divisionSettings;

        public BlockSpecificationGauge(GridDivisionSettings divisionSettings)
        {
            _divisionSettings = divisionSettings;
        }

        public IMeasurementResult TakeMeasurement(MeasurementScreenshotsSet inputSet)
        {
            var fillingInfos = new List<OneBlockSpecificationInformation>(_divisionSettings.BlockCount.X * _divisionSettings.BlockCount.Y);

            for (int bx = 0; bx < _divisionSettings.BlockCount.X; bx++)
            {
                for (int by = 0; by < _divisionSettings.BlockCount.Y; by++)
                {
                    var blockStartPosition = new IntVector2(bx * _divisionSettings.BlockSize.X, by * _divisionSettings.BlockSize.Y);
                    var allShapePixelsCount = 0;
                    var hatchPixelsCount = 0;
                    var lightIntensitySum = 0f;

                    for (int x = blockStartPosition.X;
                        x < Math.Min(blockStartPosition.X + _divisionSettings.BlockSize.X, _divisionSettings.ScreenSize.X);
                        x++)
                    {
                        for (int y = blockStartPosition.Y;
                            y < Math.Min(blockStartPosition.Y + _divisionSettings.BlockSize.Y, _divisionSettings.ScreenSize.Y);
                            y++)
                        {
                            var hatchPix = inputSet.HatchMainTexture.GetPixel(x, y);
                            if (GaugeUtils.PixelLiesOverShape(hatchPix))
                            {
                                allShapePixelsCount++;
                                if (GaugeUtils.PixelLiesOverHatchInShape(hatchPix))
                                {
                                    hatchPixelsCount++;
                                }

                                lightIntensitySum += hatchPix.g;
                            }
                        }
                    }

                    fillingInfos.Add(new OneBlockSpecificationInformation()
                    {
                        AllShapePixels = allShapePixelsCount,
                        HatchPixels = hatchPixelsCount,
                        BlockPosition = new IntVector2(bx, by),
                        LightIntensitySum = lightIntensitySum
                    });
                }
            } 
            return new BlockSpecificationResult(fillingInfos, _divisionSettings);
        }
    }

    public class BlockSpecificationResult : IMeasurementResult
    {
        private List<OneBlockSpecificationInformation> _fillingInfos;
        private GridDivisionSettings _gridDivisionSettings;

        public BlockSpecificationResult(List<OneBlockSpecificationInformation> fillingInfos, GridDivisionSettings gridDivisionSettings)
        {
            _fillingInfos = fillingInfos;
            _gridDivisionSettings = gridDivisionSettings;
        }

        public IMeasurementPOCO GeneratePoco()
        {
            return new BlockSpecificationResultPOCO()
            {
                FillingInfos = _fillingInfos
            };
        }

        public Texture2D GenerateIllustration()
        {
            var infosDict = _fillingInfos.ToDictionary(c => c.BlockPosition, c => new {c.AllShapePixels, c.HatchPixels});

            var tex = new Texture2D(_gridDivisionSettings.BlockCount.X, _gridDivisionSettings.BlockCount.Y, TextureFormat.ARGB32, false, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < _gridDivisionSettings.BlockCount.X; x++)
            {
                for (int y = 0; y < _gridDivisionSettings.BlockCount.Y; y++)
                {
                    var d = infosDict[new IntVector2(x, y)];
                    float hatchPercent = d.HatchPixels / (float) d.AllShapePixels;
                    float hatchedPixelsPercent = (float)d.AllShapePixels / (_gridDivisionSettings.BlockSize.X * _gridDivisionSettings.BlockSize.Y);
                    tex.SetPixel(x,y, new Color(hatchPercent,hatchedPixelsPercent,0,1));
                }
            }
            tex.Apply(false);
            return tex;
        }

        public string GetResultName() => "BlockSpecification";
        public string ToCsvString()
        {
            var sb = new StringBuilder();
            sb.Append("HatchPixels,AllShapePixels,LightIntensitySum,BlockPositionX,BlockPositionY");
            sb.AppendLine();

            foreach (var info in _fillingInfos)
            {
                sb.AppendLine($"{info.HatchPixels},{info.AllShapePixels},{info.LightIntensitySum},{info.BlockPosition.X},{info.BlockPosition.Y}");
            }

            return sb.ToString();
        }
    }

    [Serializable]
    public class OneBlockSpecificationInformation
    {
        public IntVector2 BlockPosition;
        public int HatchPixels;
        public int AllShapePixels;
        public float LightIntensitySum;
    }

    [Serializable]
    public class BlockSpecificationResultPOCO : IMeasurementPOCO
    {
        public List<OneBlockSpecificationInformation> FillingInfos;
    }

    public class GridDivisionSettings
    {
        public IntVector2 BlockSize;
        public IntVector2 BlockCount;
        public IntVector2 ScreenSize;
    }
}
