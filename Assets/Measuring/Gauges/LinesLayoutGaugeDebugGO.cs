using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring;
using Assets.Measuring.DebugIllustration;
using Assets.Utils;
using Assets.Utils.Textures;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

namespace Assets.Measuring.Gauges
{
    public class LinesLayoutGaugeDebugGO : MonoBehaviour
    {
        private RunOnceBox _once;
        public void Start()
        {
            _once = new RunOnceBox(() =>
            {
                FindObjectOfType<LineMeasuringPpDirectorOc>().RequireScreenshotsSet((screenshotsSet) =>
                {
                    var gauge = new LinesLayoutGauge();

                    var sw = new MyStopWatch();
                    sw.StartSegment("LineLayoutTime");
                    var result = gauge.TakeMeasurement(screenshotsSet);
                    var debIllustration = result.GenerateIllustration();

                    var poco = result.GeneratePoco();
                    Debug.Log(sw.CollectResults());
                    Debug.Log(JsonUtility.ToJson(poco));
                    File.WriteAllText(@"C:\mgr\tmp\linesLayout.json", JsonUtility.ToJson(poco));
                    GenerateDebugBalls((result as LinesLayoutResult).Curves);

                    var ppDirector = FindObjectOfType<DebugIllustrationPPDirectorOC>();
                    ppDirector.ShowIllustrations(screenshotsSet.HatchMainTexture.ToTexture2D(), debIllustration);
                });

            }, 4);
        }

        public void Update()
        {
            _once.Update();
        }


        private void GenerateDebugBalls(Dictionary<uint, CurveIn3DSpace> curves)
        {
            var root = new GameObject("RootDebugBalls");

            foreach (var pair in curves)
            {
                var curveRoot = new GameObject(pair.Key.ToString());
                curveRoot.transform.SetParent(root.transform);

                var samplesCount = 10;
                for (int i = 0; i < samplesCount; i++)
                {
                    var t = (float) i / (float) (samplesCount - 1);
                    var position = pair.Value.Sample(t);
                    var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ball.name = $"T is {t}";
                    ball.transform.SetParent(curveRoot.transform);
                    ball.transform.position = position;
                    ball.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                }
            }
        }
    }

    public class LinesLayoutGauge : IGauge
    {
        private float _maxPosition = 10;

        public IMeasurementResult TakeMeasurement(MeasurementScreenshotsSet inputSet)
        {
            Vector3[,] positionsArray = GeneratePositionsArray(inputSet.WorldPosition1Texture, inputSet.WorldPosition2Texture);
            var idArray = inputSet.IdArray;
            var occurenceArray = inputSet.HatchOccurenceArray;
            var samplesDict = GenerateSamplesDict(positionsArray, idArray, inputSet.HatchMainTexture, occurenceArray);

            var msw = new MyStopWatch();
            msw.StartSegment("Curve calc");
            var curves = CalculateCurves(samplesDict);
            //Debug.Log(msw.CollectResults());
            return new LinesLayoutResult()
            {
                WorldSpacePositionsArray = positionsArray,
                Curves = curves,
                Samples = samplesDict
            };
        }

        private struct LinesLayoutSampleWithId
        {
            public uint Id;
            public LinesLayoutSample Sample;
        }

        private Dictionary<uint, List<LinesLayoutSample>> GenerateSamplesDict(Vector3[,] positionsArray, uint[,] idArray, LocalTexture hatchMainTexture,
            bool[,] occurenceArray)
        {

            var imageSize = new IntVector2(hatchMainTexture.Width, hatchMainTexture.Height);

            return Enumerable.Range(0, imageSize.X).AsParallel().SelectMany(x =>
                {
                    return Enumerable.Range(0, imageSize.Y).Select(y =>
                    {
                        var tParam = hatchMainTexture.GetPixel(x, y).b;
                        var id = idArray[x, y];
                        if (id != 0 && occurenceArray[x, y])
                        {
                            var worldSpacePosition = positionsArray[x, y];
                            return new LinesLayoutSampleWithId()
                            {
                                Id = id,
                                Sample = new LinesLayoutSample()
                                {
                                    TParam = tParam,
                                    WorldSpacePosition = worldSpacePosition
                                }
                            };
                        }
                        else
                        {
                            return new LinesLayoutSampleWithId()
                            {
                                Id = 0,
                                Sample = null
                            };
                        }
                    });
                })
                .Where(c => c.Sample != null)
                .GroupBy(c => c.Id)
                .ToDictionary(c => c.Key, c => c.Select(r => r.Sample).ToList());

            //var outDict = new Dictionary<int, List<LinesLayoutSample>>();
            //for (int x = 0; x < imageSize.X; x++)
            //{
            //    for (int y = 0; y < imageSize.Y; y++)
            //    {
            //        var tParam = hatchIdTexture.GetPixel(x, y).b;
            //        var id = idArray[x, y];
            //        if (id != 0 && occurenceArray[x,y])
            //        {
            //            var worldSpacePosition = positionsArray[x, y];

            //            if (!outDict.ContainsKey(id))
            //            {
            //                outDict[id] = new List<LinesLayoutSample>();
            //            }

            //            outDict[id].Add(new LinesLayoutSample()
            //            {
            //                TParam = tParam,
            //                WorldSpacePosition = worldSpacePosition
            //            });
            //        }
            //    }
            //}

            //return outDict;
        }

        private Vector3[,] GeneratePositionsArray(LocalTexture worldPosition1Texture, LocalTexture worldPosition2Texture)
        {
            var imageSize = new IntVector2(worldPosition1Texture.Width, worldPosition1Texture.Height);
            var outArray = new Vector3[imageSize.X, imageSize.Y];

            Parallel.For(0, imageSize.X, x => 
                //for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    var p1 = worldPosition1Texture.GetPixel(x, y);
                    var p2 = worldPosition2Texture.GetPixel(x, y);
                    var positionX = ToFloat(p1[0], p1[1], _maxPosition);
                    var positionY = ToFloat(p1[2], p1[3], _maxPosition);
                    var positionZ = ToFloat(p2[0], p2[1], _maxPosition);

                    //CheckForOverfloat(p1[0], p1[1]);
                    //CheckForOverfloat(p1[2], p1[3]);
                    //CheckForOverfloat(p2[0], p2[1]);

                    outArray[x, y] = new Vector3(positionX, positionY, positionZ);
                }
            });

            return outArray;
        }

        private void CheckForOverfloat(float p0, float p1)
        {
            float EPSILON = 0.00001f;
            if (Mathf.Abs(p0 - 1) < EPSILON && Mathf.Abs(p1 - 1) < EPSILON)
            {
                Debug.Log("W632 There is position overflow!!");
            }
        }

        private float ToFloat(float r1, float r2, float maxPosition)
        {
            float pNorm = r1 + r2 / 256f;
            return pNorm * (2 * maxPosition) - maxPosition;
        }

        private Dictionary<uint,CurveIn3DSpace> CalculateCurves(Dictionary<uint, List<LinesLayoutSample>> samples)
        {
            var solver = new MeasurementCurveSolver();
            return samples.ToDictionary(c => c.Key, c => solver.Solve(c.Value));
        }
    }

    public class LinesLayoutResult : IMeasurementResult
    {
        public Dictionary<uint,CurveIn3DSpace> Curves;
        public Vector3[,] WorldSpacePositionsArray;
        public Dictionary<uint, List<LinesLayoutSample>> Samples { get; set; }

        public IMeasurementPOCO GeneratePoco()
        {
            return new LinesLayoutPOCO()
            {
                Infos = Curves.Select(c => new StrokeCurveInfo()
                {
                    Id = c.Key,
                    XCoefficients = c.Value.XCoefficients,
                    YCoefficients = c.Value.YCoefficients,
                    ZCoefficients = c.Value.ZCoefficients
                }).ToList()
            };
        }

        public Texture2D GenerateIllustration()
        {
            var imageSize = new IntVector2(WorldSpacePositionsArray.GetLength(0), WorldSpacePositionsArray.GetLength(1));
            var tex = new Texture2D(imageSize.X, imageSize.Y, TextureFormat.RGBA32,false,false);
            var max = 10f;

            for (int x = 0; x < imageSize.X; x++)
            {
                for (int y = 0; y < imageSize.Y; y++)
                {
                    var pos = WorldSpacePositionsArray[x, y];
                    var color = new Color(pos.x / max, pos.y / max, pos.z / max, 1);

                    tex.SetPixel(x,y,color);
                }
            }
            tex.Apply();
            return tex;
        }
        public string GetResultName() => "LinesLayout";

        public string ToCsvString()
        {
            var sb = new StringBuilder();
            sb.Append("Id,X0,X1,X2,X3,X4,Y0,Y1,Y2,Y3,Y4,Z0,Z1,Z2,Z3,Z4");
            sb.AppendLine();

            foreach (var pair in Curves)
            {
                var id = pair.Key;
                var curve = pair.Value;

                sb.AppendLine($"{id}," 
                              + $"{curve.XCoefficients[0]},{curve.XCoefficients[1]},{curve.XCoefficients[2]},{curve.XCoefficients[3]},{curve.XCoefficients[4]},"
                              + $"{curve.YCoefficients[0]},{curve.YCoefficients[1]},{curve.YCoefficients[2]},{curve.YCoefficients[3]},{curve.YCoefficients[4]},"
                              + $"{curve.ZCoefficients[0]},{curve.ZCoefficients[1]},{curve.ZCoefficients[2]},{curve.ZCoefficients[3]},{curve.ZCoefficients[4]}");
            }

            return sb.ToString();
        }
    }

    public class LinesLayoutSample
    {
        public float TParam;
        public Vector3 WorldSpacePosition;
    }

    [Serializable]
    public class LinesLayoutPOCO : IMeasurementPOCO
    {
        public List<StrokeCurveInfo> Infos;
    }

    [Serializable]
    public class StrokeCurveInfo
    {
        public uint Id;
        public float[] XCoefficients;
        public float[] YCoefficients;
        public float[] ZCoefficients;
    }


    public class MeasurementCurveSolver
    {
        private int _polynomialDegree = 5;

        public CurveIn3DSpace Solve(List<LinesLayoutSample> samples)
        {
            var A = Matrix<float>.Build.Dense(_polynomialDegree, _polynomialDegree);
            var CX = Matrix<float>.Build.Dense(_polynomialDegree, 1);
            var CY = Matrix<float>.Build.Dense(_polynomialDegree, 1);
            var CZ = Matrix<float>.Build.Dense(_polynomialDegree, 1);

            //var tVectors = samples.Select(s => CalculateTVector(s.TParam)).ToList();
            //CX = samples.AsParallel().Select((c, i) => tVectors[i] * c.WorldSpacePosition.x).Aggregate(CX, ((a, b) => a + b));
            //CY = samples.AsParallel().Select((c, i) => tVectors[i] * c.WorldSpacePosition.x).Aggregate(CY, ((a, b) => a + b));
            //CZ = samples.AsParallel().Select((c, i) => tVectors[i] * c.WorldSpacePosition.x).Aggregate(CZ, ((a, b) => a + b));
            //A = samples.AsParallel().Select((c, i) => (tVectors[i] * tVectors[i].Transpose())).Aggregate(A, ((a, b) => a + b));

            foreach (var aSample in samples)
            {
                var tVector = CalculateTVector(aSample.TParam);
                var T = tVector * tVector.Transpose();
                A = A + T;

                CX = CX + tVector * aSample.WorldSpacePosition.x;
                CY = CY + tVector * aSample.WorldSpacePosition.y;
                CZ = CZ + tVector * aSample.WorldSpacePosition.z;
            }

            var toOut = new CurveIn3DSpace()
            {
                XCoefficients = Conjgrad(A, CX).ToColumnMajorArray(),
                YCoefficients = Conjgrad(A, CY).ToColumnMajorArray(),
                ZCoefficients = Conjgrad(A, CZ).ToColumnMajorArray()
            };
            return toOut;
        }

        private Matrix<float> CalculateTVector(float t)
        {
            var array = Enumerable.Range(0, _polynomialDegree).Select(i => Mathf.Pow(t, i)).ToArray();

            return Vector<float>.Build.DenseOfArray(array).ToColumnMatrix();
        }

        private Matrix<float> Conjgrad(Matrix<float> A, Matrix<float> b)
        {
            var x0 = Matrix<float>.Build.Dense(_polynomialDegree, 1);

            var r = b - (A * x0);
            var w = -r; //this is pk
            var z = (A * w); // this is A*pk
            float a = ((r.Transpose() * w)).At(0, 0) / (((w).Transpose() * z)).At(0, 0);
            var x = x0 + a * w;

            for (int i = 0; i < 4; i++)
            {
                r = r - a * z;
                if (r.L2Norm() < 0.00001)
                {
                    break;
                }

                float B = ((r).Transpose() * z).At(0, 0) / ((w.Transpose()) * z).At(0, 0);
                w = -r + B * w;
                z = (A * w);
                a = ((r).Transpose() * w).At(0, 0) / ((w).Transpose() * z).At(0, 0);
                x = x + a * w;
            }

            return x;
        }
    }

    public class CurveIn3DSpace
    {
        public float[] XCoefficients;
        public float[] YCoefficients;
        public float[] ZCoefficients;

        public Vector3 Sample(float t)
        {
            var x = XCoefficients.Select((c, i) => c * Mathf.Pow(t, i)).Sum();
            var y = YCoefficients.Select((c, i) => c * Mathf.Pow(t, i)).Sum();
            var z = ZCoefficients.Select((c, i) => c * Mathf.Pow(t, i)).Sum();

            return new Vector3(x,y,z);
        }
    }
}
