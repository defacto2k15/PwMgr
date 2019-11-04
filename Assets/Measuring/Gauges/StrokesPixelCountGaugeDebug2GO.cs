using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring.DebugIllustration;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Measuring.Gauges
{
    public class StrokesPixelCountGaugeDebug2GO : MonoBehaviour
    {
         private RunOnceBox _once;
        public void Start()
        {
            _once = new RunOnceBox(() =>
            {
                FindObjectOfType<LineMeasuringPpDirectorOc>().RequireScreenshotsSet((screenshotsSet) =>
                {
                    var gauge = new StrokesPixelCountGauge();

                    var sw = new MyStopWatch();
                    sw.StartSegment("PixelCountTime");
                    var result = gauge.TakeMeasurement(screenshotsSet);
                    var debIllustration = result.GenerateIllustration();

                    var poco = result.GeneratePoco();
                    Debug.Log(sw.CollectResults());
                    Debug.Log(JsonUtility.ToJson(poco));
                    File.WriteAllText(@"C:\mgr\tmp\pixelCount.json", JsonUtility.ToJson(poco));

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

    public class StrokesPixelCountGauge : IGauge
    {
        public IMeasurementResult TakeMeasurement(MeasurementScreenshotsSet inputSet)
        {
            var occurenceArray = inputSet.HatchOccurenceArray;
            var idsArray = inputSet.IdArray;
            var tParams = GeneratePixelCountsDict(inputSet.HatchMainTexture, occurenceArray,idsArray);
            return new StrokesPixelCountResult(){TParamsPerStroke = tParams, IdsArray = idsArray, OccurenceArray = occurenceArray};
        }

        private Dictionary<uint,List<float>> GeneratePixelCountsDict(LocalTexture hatchMainTexture, bool[,] occurenceArray, uint[,] idsArray)
        {

            var imageSize = new IntVector2(hatchMainTexture.Width, hatchMainTexture.Height);
            var strokesDict = new Dictionary<uint, List<float>>();
            for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    var tParam = hatchMainTexture.GetPixel(x, y).b;
                    var id = idsArray[x, y];
                    if (occurenceArray[x, y] && id != 0)
                    {
                        if (!strokesDict.ContainsKey(id))
                        {
                            strokesDict[id] = new List<float>();
                        }
                        strokesDict[id].Add(tParam);
                    }
                }
            }

            return strokesDict;
        }
    }

    public class StrokesPixelCountResult : IMeasurementResult
    {
        public Dictionary<uint,List<float>> TParamsPerStroke;
        public uint[,] IdsArray;
        public bool[,] OccurenceArray;

        public IMeasurementPOCO GeneratePoco()
        {
            return new StrokesPixelsCountPOCO()
            {
                Strokes = TParamsPerStroke.Select(c => new OneStrokePixelCountResult(){Id = c.Key, TParams = c.Value}).ToList()
            };
        }

        public Texture2D GenerateIllustration()
        {
            var imageSize = new IntVector2(IdsArray.GetLength(0), IdsArray.GetLength(1));
            var tex = new Texture2D(imageSize.X, imageSize.Y, TextureFormat.RGBA32,false,false);
            var maxPixelCount = TParamsPerStroke.Select(c => c.Value.Count).DefaultIfEmpty(0).Max();

            for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    var id = IdsArray[x,y];
                    if (OccurenceArray[x, y] && id != 0)
                    {
                        var countFactor = 0f;
                        if (maxPixelCount != 0)
                        {
                            countFactor = TParamsPerStroke[id].Count / (float) (maxPixelCount);
                        }

                        tex.SetPixel(x,y, new Color(countFactor, 1,0,1));
                    }
                    else
                    {
                        tex.SetPixel(x,y, new Color(1,0,1,1));
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        public string GetResultName() => "StrokesPixelCount";

        public string ToCsvString()
        {
            var sb = new StringBuilder();

            foreach (var pair in TParamsPerStroke)
            {
                sb.Append(pair.Key);
                foreach (var tParam in pair.Value)
                {
                    sb.Append("," + tParam);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

    }

    [Serializable]
    public class OneStrokePixelCountResult
    {
        public uint Id;
        public List<float> TParams;
    }

    [Serializable]
    public class StrokesPixelsCountPOCO : IMeasurementPOCO
    {
        public List<OneStrokePixelCountResult> Strokes;
    }
}
