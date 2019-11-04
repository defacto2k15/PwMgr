using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Imaging.Filters;
using Accord.Math;
using Assets.Measuring.DebugIllustration;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Assets.Measuring.Gauges
{
    public class LinesWidthGaugeDebugGO : MonoBehaviour
    {
         private RunOnceBox _once;

        public void Start()
        {
            _once = new RunOnceBox(() =>
            {
                FindObjectOfType<LineMeasuringPpDirectorOc>().RequireScreenshotsSet((screenshotsSet) =>
                {
                    Preconditions.Fail("ADD MATERIAL ");
                    var gauge = new LinesWidthGauge(null);

                    var sw = new MyStopWatch();
                    sw.StartSegment("LineWidthTime");
                    var result = gauge.TakeMeasurement(screenshotsSet);
                    var debIllustration = result.GenerateIllustration();

                    var poco = result.GeneratePoco();
                    Debug.Log(sw.CollectResults());
                    Debug.Log(JsonUtility.ToJson(poco));
                    File.WriteAllText(@"C:\mgr\tmp\linesWidth.json", JsonUtility.ToJson(poco));

                    var ppDirector = FindObjectOfType<DebugIllustrationPPDirectorOC>();
                    ppDirector.ShowIllustrations(screenshotsSet.HatchMainTexture.ToTexture2D(), debIllustration);
                });

            }, 4);
        }

        public void Update()
        {
            _once.Update();
        }
       
    }

    public class LinesWidthGauge : IGauge
    {
        private IntVector2 _distancesWindowSize = new IntVector2(32,32);
        private IntVector2 _localMaximaWindowSize = new IntVector2(8,8);
        private HatchSkeletonizer _skeletonizer;
        private HashSet<uint> _lastIds;

        public LinesWidthGauge(Material skeletonizerMaterial)
        {
            _skeletonizer = new HatchSkeletonizer(skeletonizerMaterial, 20);
            _lastIds = new HashSet<uint>();
        }

        private bool _initializedSkeletonizer = false;
        public IMeasurementResult TakeMeasurement(MeasurementScreenshotsSet inputSet)
        {
            if (!_initializedSkeletonizer)
            {
                _initializedSkeletonizer = true;
                _skeletonizer.Initialize();
            }

            var mainTexture = inputSet.HatchMainTexture;

            var distancesArray = GenerateDistancesArray(mainTexture);
            //var localMaximaArray = GenerateLocalMaximaArray(inputSet.HatchMainTexture2D);
            var localMaximaArray = OldGenerateLocalMaximaArray(inputSet.HatchMainTexture, distancesArray);
            var idArray = inputSet.IdArray;

            bool[,] idChangesArray = GenerateIdChangesArray(idArray);
            foreach (var id in idArray)
            {
                if (id != 0)
                {
                    _lastIds.Add(id);
                }
            }

            return new LinesWidthResult()
            {
                DistancesArray = distancesArray,
                LocalMaximaArray = localMaximaArray,
                OutIdArray = idArray,
                IdChagnesArray = idChangesArray
            };
        }

        private bool[,] GenerateIdChangesArray(uint[,] thisIdArray)
        {
            var outArray = new bool[thisIdArray.GetLength(0), thisIdArray.GetLength(1)];
            Parallel.For(0, outArray.GetLength(0), x =>
                //for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < outArray.GetLength(1); y++)
                {
                    if (thisIdArray[x, y] != 0 && !_lastIds.Contains(thisIdArray[x,y]))
                    {
                        outArray[x, y] = true;
                    }
                    else
                    {
                        outArray[x, y] = false;
                    }

                }
            });

            return outArray;
        }

        private float[,] GenerateDistancesArray(LocalTexture mainTexture)
        {
            var imageSize = new IntVector2(mainTexture.Width, mainTexture.Height);
            var outDistanceArray = new float[imageSize.X, imageSize.Y];

            Parallel.For(0, imageSize.X, x => 
            //for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    var centerPixel = mainTexture.GetPixel(x, y);
                    if (!GaugeUtils.PixelLiesOverShapeAndHatch(centerPixel))
                    {
                        outDistanceArray[x, y] = 0f;
                    }
                    else
                    {
                        float minimalDistance = (_distancesWindowSize.ToFloatVec() * 0.5f).magnitude;

                        for (int ix = Math.Max(0, x - _distancesWindowSize.X / 2); ix < Math.Min(imageSize.X, x + _distancesWindowSize.X / 2); ix++)
                        {
                            for (int iy = Math.Max(0, y - _distancesWindowSize.Y / 2); iy < Math.Min(imageSize.Y, y + _distancesWindowSize.Y / 2); iy++)
                            {
                                var p = mainTexture.GetPixel(ix, iy);
                                if (!GaugeUtils.PixelLiesOverShapeAndHatch(p))
                                {
                                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(ix, iy));
                                    minimalDistance = Mathf.Min(distance, minimalDistance);
                                }
                            }

                        }

                        outDistanceArray[x, y] =  minimalDistance;
                    }

                }
            });

            return outDistanceArray;
        }

        private bool[,] GenerateLocalMaximaArray(Texture2D mainTexture)
        {
            float EPSILON = 0.00001f;
            var imageSize = new IntVector2(mainTexture.width, mainTexture.height);
            var outArray = new bool[imageSize.X, imageSize.Y];

            var map = _skeletonizer.Skeletonize(mainTexture);
            Parallel.For(0, imageSize.X, x => 
            //for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    if (map.GetPixel(x, y).r < 0.1)
                    {
                        outArray[x, y] = true;
                    }
                    else
                    {
                        outArray[x, y] = false;
                    }
                }
            });

            return outArray;
        }


        private bool[,] OldGenerateLocalMaximaArray(LocalTexture mainTexture, float[,] distancesArray)
        {
            float EPSILON = 0.00001f;
            var imageSize = new IntVector2(mainTexture.Width, mainTexture.Height);
            var outArray = new bool[imageSize.X, imageSize.Y];

            Parallel.For(0, imageSize.X, x =>
            //for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    var centerPixelDistance = distancesArray[x, y];
                    var centerPixel = mainTexture.GetPixel(x, y);
                    if (!GaugeUtils.PixelLiesOverShapeAndHatch(centerPixel))
                    {
                        outArray[x, y] = false;
                    }
                    else
                    {
                        float maximalDistance = float.MinValue;
                        int biggerPixelsCount=0;
                        int allPixelsCount = 0;

                        for (int ix = Math.Max(0, x - _localMaximaWindowSize.X / 2); ix < Math.Min(imageSize.X, x + _localMaximaWindowSize.X / 2); ix++)
                        {
                            for (int iy = Math.Max(0, y - _localMaximaWindowSize.Y / 2); iy < Math.Min(imageSize.Y, y + _localMaximaWindowSize.Y / 2); iy++)
                            {
                                allPixelsCount++;
                                var d = distancesArray[ix, iy];
                                if (d > 0)
                                {
                                    if (centerPixelDistance < d)
                                    {
                                        biggerPixelsCount++;
                                    }
                                    maximalDistance = Mathf.Max(d, maximalDistance);
                                }
                            }

                        }

                        bool isLocalMaxima = Mathf.Abs(maximalDistance - centerPixelDistance) < EPSILON;
                        isLocalMaxima = false;
                        if (((float) biggerPixelsCount) / ((float) allPixelsCount) < 0.1)
                        {
                            isLocalMaxima = true;
                        }
                        outArray[x, y] = isLocalMaxima;
                    }

                }
            });

            return outArray;
        }

    }

    public class LinesWidthResult : IMeasurementResult
    {
        public float[,] DistancesArray;
        public bool[,] LocalMaximaArray;
        public uint[,] OutIdArray;
        public bool[,] IdChagnesArray;

        public IMeasurementPOCO GeneratePoco()
        {
            var outDict = new Dictionary<uint, List<float>>();

            var width = DistancesArray.GetLength(0);
            var height = DistancesArray.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var d = DistancesArray[x, y];
                    if (d > 0 && LocalMaximaArray[x,y])
                    {
                        var id = OutIdArray[x, y];
                        if (!outDict.ContainsKey(id))
                        {
                            outDict[id] = new List<float>();
                        }

                        outDict[id].Add(d);
                    }
                }
            }
            return new LinesWidthPOCO()
            {
                StrokeWidthInfos = outDict.Select(c => new StrokeWidthInfo(){Id = c.Key, Widths = c.Value}).ToList()
            };
        }

        public Texture2D GenerateIllustration()
        {
            var width = DistancesArray.GetLength(0);
            var height = DistancesArray.GetLength(1);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32,false,false);

            float maxDistance = DistancesArray.Max();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool idChange = false;
                    if (IdChagnesArray != null)
                    {
                        idChange = IdChagnesArray[x, y];
                    }
                    tex.SetPixel(x,y, 
                        new Color( DistancesArray[x,y] / maxDistance,
                            LocalMaximaArray[x,y] ? 1 : 0,
                            idChange ? 1 : 0,
                            1));
                }
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            return tex;
        }
        public string GetResultName() => "LinesWidth";

        public string ToCsvString()
        {
            var sb = new StringBuilder();
            var poco = GeneratePoco() as LinesWidthPOCO;

            foreach (var info in poco.StrokeWidthInfos)
            {
                sb.Append(info.Id);
                foreach (var width in info.Widths)
                {
                    sb.Append("," + width);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

    }

    [Serializable]
    public class LinesWidthPOCO : IMeasurementPOCO
    {
        public List<StrokeWidthInfo> StrokeWidthInfos;
    }

    [Serializable]
    public class StrokeWidthInfo
    {
        public uint Id;
        public List<float> Widths;
    }
}
