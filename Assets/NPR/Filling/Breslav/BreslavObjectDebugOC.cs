using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.NPR.Curvature;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Filling.Breslav
{
    public class BreslavObjectDebugOC : MonoBehaviour
    {
        public BreslavConfiguration Configuration;  
        public bool DrawDebugBalls;
        private List<GameObject> _primitives = new List<GameObject>();
        private BreslavTextureTransitionGenerator _transitionGenerator;
        private Material _material;

        public void Start()
        {
            _material = GetComponent<MeshRenderer>().material;
            _transitionGenerator = new BreslavTextureTransitionGenerator(new RandomBreslavSamplePointsProvide(), Configuration);
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            var cam = FindObjectOfType<Camera>();
            _transitionGenerator.Initialize(mesh, cam,transform, _material);

            var ltw = transform.localToWorldMatrix;
            if (DrawDebugBalls)
            {
                int i1 = 0;
                foreach (var point in _transitionGenerator.Samples)
                {
                    var primitive = AddPrimitive(ltw.MultiplyPoint(point.Position), ltw.MultiplyVector(point.Normal), new Color(0, 0, 0), gameObject, new Vector3(0.1f, 0.1f, 0.1f), i1);
                    _primitives.Add(primitive);
                    var surroundPoints = _transitionGenerator.ComputeSurroundPoints(point, cam, transform);
                    foreach (var x1 in surroundPoints.Select((a, i) => new {a, i}))
                    {
                        AddPrimitive(x1.a, new Vector3(0, 0, 0), Color.blue, primitive, new Vector3(0.4f, 0.4f, 0.4f), x1.i);
                    }

                    i1++;
                }
            }
        }

        private GameObject AddPrimitive(Vector3 position, Vector3 rotation, Color color, GameObject parent, Vector3 scale, int index)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            primitive.transform.parent = parent.transform;
            primitive.transform.position= position;
            primitive.transform.rotation = Quaternion.LookRotation(rotation);
            primitive.transform.localScale = scale;
            primitive.GetComponent<MeshRenderer>().material.color = color;
            primitive.name = index.ToString();
            return primitive;
        }


        public void Update()
        {
            var cam = FindObjectOfType<Camera>();
            var transition = _transitionGenerator.Update(cam, transform);
            transition.SetInMaterial(_material);

            if (DrawDebugBalls ) { 
                 var maxWeight = _transitionGenerator.Weights.Max();
                for (int i = 0; i < _transitionGenerator.Weights.Count; i++)
                {
                    var weight = _transitionGenerator.Weights[i];
                    _primitives[i].name = weight.ToString();
                    if (weight > 0)
                    {
                        var weightPercent = weight / maxWeight;
                        _primitives[i].GetComponent<MeshRenderer>().material.color = new Color(weightPercent, 0, 0);
                    }
                    else
                    {
                        _primitives[i].GetComponent<MeshRenderer>().material.color = new Color(0, 1, 0);
                    }
                }
            }
        }

    }


    public class BreslavTextureTransitionGenerator
    {
        private IBreslavSamplePointsProvider _samplePointsProvider;
        private BreslavConfiguration _configuration;

        private BreslavFrameInfo _lastFrameInfo;
        private List<PointWithNormal> _samples;
        private BreslavTextureTransition _lastTransition;

        public BreslavTextureTransitionGenerator(IBreslavSamplePointsProvider samplePointsProvider, BreslavConfiguration configuration)
        {
            _samplePointsProvider = samplePointsProvider;
            _configuration = configuration;
            _lastTransition = new BreslavTextureTransition()
            {
                O = Vector2.zero,
                U = new Vector2(0, 1),
                V = new Vector2(1, 0),
                St = 0
            };
        }

        public List<PointWithNormal> Samples => _samples;
        public List<float> Weights => _lastFrameInfo.Weights;

        public void Initialize(Mesh mesh, Camera cam, Transform parentObjectTransform, Material material)
        {
            _samples = _samplePointsProvider.ProvideSamples(_configuration.SamplesCount, _configuration.SamplesMinimumDistance, mesh);
            InitializeLastFrameData(cam, parentObjectTransform);

            if (_configuration.UseLod)
            {
                material.SetFloat("_UseBreslavScalling", 1);
            }
            else
            {
                material.SetFloat("_UseBreslavScalling", 0);
            }
        }

        private void InitializeLastFrameData(Camera cam, Transform parentObjectTransform)
        {
            var screenPosOfPoints = _samples.Select(c => cam.WorldToViewportPoint(c.Position).XY()).ToList();
            var weights = ComputeWeightsOfPoints(_samples, cam, parentObjectTransform);
            _lastFrameInfo = new BreslavFrameInfo()
            {
                SamplePointsScreenPositions = screenPosOfPoints,
                Weights = weights,
                O = new Vector2(0f, 0f),
                U = new Vector2(1, 0),
                V = new Vector2(0, 1),
                LodInfo = new BreslavLodFrameInfo()
                {
                    Scale = 1,
                }
            };
        }

        private List<float> ComputeWeightsOfPoints(List<PointWithNormal> pointWithNormals, Camera cam, Transform objectTransform)
        {
            var weights = new List<float>();
            foreach (var point in pointWithNormals)
            {
                var points = ComputeSurroundPoints(point,cam,objectTransform)
                    .Select(c => cam.WorldToViewportPoint(c))
                    .ToList();

                var worldPointNormal = objectTransform.localToWorldMatrix.MultiplyVector(point.Normal);
                var worldPointPosition = objectTransform.localToWorldMatrix.MultiplyPoint(point.Position);
                var cosFi = Vector3.Dot(worldPointNormal, worldPointPosition - cam.transform.position);
                var sp = cam.WorldToViewportPoint(worldPointPosition);
                if (cosFi < 0 && sp.x > 0 && sp.x < 1 && sp.y > 0 && sp.y < 1)
                    //points.Max(c => Math.Min(c.x, c.y)) >= 0 && points.Min(c => Math.Max(c.x, c.y)) <= 1 && points.Min(c => c.z >= 0))
                {
                    var area = ComputeArea(points);
                    weights.Add(area);
                }
                else
                {
                    weights.Add(0);
                }
            }
            return weights;
        }

        public List<Vector3> ComputeSurroundPoints(PointWithNormal point, Camera cam, Transform objectTransform)
        {
            var surroundingDistance = _configuration.SorroundingDistance;
            var trs = Matrix4x4.TRS(point.Position, Quaternion.LookRotation(point.Normal), new Vector3(1, 1, 1) * surroundingDistance);

            var p3 = new List<Vector3>()
                {
                        new Vector3(1, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(-1, 0, 0),
                        new Vector3(0, -1, 0),
                }.Select(c => trs.MultiplyPoint(c))
                .Select(c => objectTransform.localToWorldMatrix.MultiplyPoint(c))
                .ToList();
            return p3;
        }


        private static float ComputeArea(List<Vector3> inPoints)
        {
            var points = new List<Vector3>(inPoints);
            points.Add(points[0]);
            var area = Math.Abs(points.Take(points.Count - 1).Select((p, i) => (points[i + 1].x - p.x) * (points[i + 1].y + p.y)).Sum() / 2);
            return Math.Abs(area);
        }

        public BreslavTextureTransition Update(Camera cam, Transform parentObjectTransform)
        {
            var baseWeights = ComputeWeightsOfPoints(_samples, cam, parentObjectTransform);

            var that = this;
            var weights = Enumerable.Range(0, _samples.Count).Select(i =>
            {
                var oldW = that._lastFrameInfo.Weights[i];
                var newW = baseWeights[i];
                if (oldW > 0 && newW > 0) // we only consider points that are visible in this and previous frames
                {
                    return newW;
                }
                else
                {
                    return 0;
                }
            }).ToList();
            var anyNotZero = weights.Any(c => c > 0);
            //Debug.Log(anyNotZero + );

            var screenPosOfPoints = _samples.Select(c => cam.WorldToViewportPoint(parentObjectTransform.localToWorldMatrix.MultiplyPoint(c.Position))).Select(c => new Vector2(c.x, c.y)).ToList();
            if (!anyNotZero)
            {
                _lastFrameInfo.Weights = baseWeights;
                _lastFrameInfo.SamplePointsScreenPositions = screenPosOfPoints;
                return _lastTransition;
            }

            var currentCentertoid = screenPosOfPoints.Select((c, i) => c * weights[i]).Aggregate((a, b) => a + b) / weights.Sum();
            var currentDeltaPositions = screenPosOfPoints.Select(c => c - currentCentertoid).ToList();

            var previousCentertoid = _lastFrameInfo.SamplePointsScreenPositions.Select((c, i) => c * weights[i]).Aggregate((a, b) => a + b) / weights.Sum();
            var previousDeltaPositions = _lastFrameInfo.SamplePointsScreenPositions.Select(c => c - previousCentertoid).ToList();

            Vector2 z;
            float zScaleMultiplier = 0;

            if (_configuration.ZComputingMode == BreslavZComputingMode.PaperMode)
            {
                var left = Enumerable.Range(0, _samples.Count).Select(i =>
                    new Vector2(
                        Vector2.Dot(weights[i] * previousDeltaPositions[i], currentDeltaPositions[i]),
                        VectorUtils.CrossProduct(weights[i] * previousDeltaPositions[i], currentDeltaPositions[i])
                    )).Aggregate((a, b) => a + b);

                var right = Enumerable.Range(0, _samples.Count).Select(i => weights[i] * Math.Pow(previousDeltaPositions[i].magnitude, 2))
                    .Aggregate((a, b) => a + b);
                var _z = left / (float) right;

                var s = (1 / _z.magnitude) * Math.Sqrt(
                            Enumerable.Range(0, _samples.Count).Select(i => weights[i] * Math.Pow(previousDeltaPositions[i].magnitude, 2))
                                .Aggregate((a, b) => a + b) /
                            Enumerable.Range(0, _samples.Count).Select(i => weights[i] * Math.Pow(currentDeltaPositions[i].magnitude, 2))
                                .Aggregate((a, b) => a + b)
                        );

                if (!_configuration.DoRotation)
                {
                    _z = new Vector2(_z[0], 0);
                }

                zScaleMultiplier = _z.magnitude;
                _z *= (float) s;

                z = _z;
            }
            else
            {
                double so = 0;
                double sn = 0;
                Vector2 _z = new Vector2(0, 0);
                for (int i = 0; i < _samples.Count; i++)
                {
                    if (weights[i] > 0)
                    {
                        var m_o = previousDeltaPositions[i];
                        var m_n = currentDeltaPositions[i];
                        float m_w = weights[i];

                        _z += new Vector2( (float)Cm2(m_o, m_n), (float)Det(m_o, m_n)) * m_w;
                        so += m_o.sqrMagnitude * m_w;
                        sn += m_n.sqrMagnitude * m_w;
                    }
                }

                if (_configuration.DoSymmetricScalling)
                {
                    _z = _z.normalized * Mathf.Sqrt((float) (sn / so));
                }
                else
                {
                    _z /= ((float)so);
                }
                zScaleMultiplier = _z.magnitude;
                z = _z;

                if (!_configuration.DoRotation)
                {
                    z = new Vector2(z[0], 0);
                }

                double EPSILON = 0.00000000001;
                if (Math.Abs(so) <= EPSILON|| Math.Abs(sn) <= EPSILON || _z.magnitude <= EPSILON)
                {
                    _lastFrameInfo = new BreslavFrameInfo()
                    {
                        O = _lastFrameInfo.O,
                        U = _lastFrameInfo.U,
                        V = _lastFrameInfo.V,
                        SamplePointsScreenPositions = screenPosOfPoints,
                        Weights = baseWeights,
                        LodInfo = _lastFrameInfo.LodInfo
                    };

                    return _lastTransition;
                }
            }

            var lodInfo = _lastFrameInfo.LodInfo.Clone();
            lodInfo.Scale *= zScaleMultiplier;

            // interpolation parameter between 2 LOD scales
            float st = 0;

            var o = currentCentertoid + ComplexMultiplication(_lastFrameInfo.O - previousCentertoid, z);
            var u = ComplexMultiplication(_lastFrameInfo.U, z);
            var v = ComplexMultiplication(_lastFrameInfo.V, z);

            if (_configuration.UseLod)
            {
                float locScale = lodInfo.Scale;
                Preconditions.Assert(locScale > 0, $"LocScale <= 0: {locScale}");
                while (locScale >= 2) //Ensuring s is [1,2)
                {
                    locScale /= 2;
                }
                while (locScale < 1)
                {
                    locScale *= 2;
                }

                if (locScale < _configuration.Lod_St0)
                {
                    st = 0;
                }
                else if (locScale > _configuration.Lod_St1)
                {
                    st = 1;
                }
                else
                {
                    st = (locScale - _configuration.Lod_St0) / (_configuration.Lod_St1 - _configuration.Lod_St0);
                }
                u = u.normalized * locScale;
                v = v.normalized * locScale;
            }

            _lastFrameInfo = new BreslavFrameInfo()
            {
                O = o,
                U = u,
                V = v,
                SamplePointsScreenPositions = screenPosOfPoints,
                Weights = baseWeights,
                LodInfo = lodInfo
            };
            return new BreslavTextureTransition()
            {
                V = v,
                O = o,
                St = st,
                U = u,
                LodScale = lodInfo.Scale
                
            };
        }

        private Vector2 ComplexMultiplication(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x * v2.x - v1.y * v2.y, v1.x * v2.y + v1.y * v2.x);
        }

        // this should be crossProduct, but implementation in jot-lib uses this
        private double Cm2(Vector2 v1, Vector2 v2)
        {
            return ( ((double)v1.x) * ((double)v2.x) + ((double)v1.y) * ((double)v2.y));
        }

        //        Returns the 'z' coordinate of the cross product of the  two vectors.
        private double Det(Vector2 v1, Vector2 v2)
        {
            return ((double)v1[0]) * ((double)v2[1]) - ((double)v1[1]) * ((double)v2[0]);
        }

    }


    public class BreslavTextureTransition
    {
        public Vector2 O;
        public Vector2 U;
        public Vector2 V;
        public float St;
        public float LodScale;

        public void SetInMaterial(Material mat)
        {
            mat.SetVector("_BreslavO", O);
            mat.SetVector("_BreslavU", U);
            mat.SetVector("_BreslavV", V);
            mat.SetFloat("_BreslavSt", St);
            mat.SetFloat("_LodScale", LodScale);
        }
    }

    public interface IBreslavSamplePointsProvider
    {
        List<PointWithNormal> ProvideSamples(int count, float minimumDistance, Mesh mesh);
    }

    public class RandomBreslavSamplePointsProvide : IBreslavSamplePointsProvider
    {
        public List<PointWithNormal> ProvideSamples(int samplesCount, float minimumDistance, Mesh mesh)
        {
            var unaliasedGenerator = new UnaliasedMeshGenerator();
            var unaliasedMesh = unaliasedGenerator.GenerateUnaliasedMesh(mesh);

            var verticesFlatArray = unaliasedMesh.Vertices.SelectMany(c => VectorUtils.ToArray((Vector3) c)).ToArray();
            var unaliasedVerticesCount = unaliasedMesh.Vertices.Length;
            var unaliasedTrianglesCount = unaliasedMesh.Triangles.Length / 3;

            var flatOutSamplesArray = new float[samplesCount * 3];
            var outOwningTriangleArray = new int[samplesCount];

            PrincipalCurvatureDll.EnableLogging();
            int callStatus = 0;
            var randomPointsMaxTriesCount = 100;
            for (int i = 0; i < randomPointsMaxTriesCount; i++)
            {
                 callStatus = PrincipalCurvatureDll.random_points_on_mesh(verticesFlatArray, unaliasedVerticesCount, unaliasedMesh.Triangles,
                    unaliasedTrianglesCount, samplesCount, flatOutSamplesArray, outOwningTriangleArray);
                if (callStatus != 0)
                {
                    Debug.Log($" PrincipalCurvatureDll.random_points_on_mesh call failed with status {callStatus}, retrying");
                }
                else
                {
                    break;
                }
            }
            Preconditions.Assert(callStatus == 0, "Calling random_points_on_mesh failed, as returned status " + callStatus);

            var pointWithNormals = new List<PointWithNormal>();
            for (int i = 0; i < samplesCount; i++)
            {
                var point = new Vector3(flatOutSamplesArray[i * 3 + 0], flatOutSamplesArray[i * 3 + 1], flatOutSamplesArray[i * 3 + 2]);
                var triangleIndex = outOwningTriangleArray[i];

                var v1 = unaliasedMesh.Vertices[unaliasedMesh.Triangles[triangleIndex * 3 + 0]];
                var v2 = unaliasedMesh.Vertices[unaliasedMesh.Triangles[triangleIndex * 3 + 1]];
                var v3 = unaliasedMesh.Vertices[unaliasedMesh.Triangles[triangleIndex * 3 + 2]];

                var normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                pointWithNormals.Add(new PointWithNormal() {Normal = normal, Position = point});
            }

            for (int i = 0; i < pointWithNormals.Count; i++)
            {
                var current = pointWithNormals[i];
                var itemsToRemove = pointWithNormals.Select((c, j) => new {c, j})
                    .Where(c => c.j != i && Vector3.Distance(c.c.Position, current.Position) < minimumDistance).Select(c => c.j).ToList();

                for (int j = itemsToRemove.Count - 1; j >= 0; j--)
                {
                    pointWithNormals.RemoveAt(itemsToRemove[j]);
                }
            }

            return pointWithNormals;
        }
    }

    public class PointWithNormal
    {
        public Vector3 Position;
        public Vector3 Normal;
    }

    public class BreslavFrameInfo
    {
        public List<Vector2> SamplePointsScreenPositions;
        public List<float> Weights;
        public Vector2 O;
        public Vector2 U;
        public Vector2 V;
        public BreslavLodFrameInfo LodInfo;
    }

    public class BreslavLodFrameInfo
    {
        public float Scale;

        public BreslavLodFrameInfo Clone()
        {
            return new BreslavLodFrameInfo()
            {
                Scale = Scale,
            };
        }
    }

    [Serializable]
    public class BreslavConfiguration
    {
        public BreslavZComputingMode ZComputingMode;
        public bool DoSymmetricScalling;

        public bool DoRotation;

        //public bool UseDirectionVec;
        public bool UseLod;

        public float Lod_St0 = 1.3f;
        public float Lod_St1 = 1.7f;

        public int SamplesCount = 1000;
        public float SamplesMinimumDistance = 0.2f;
        public float SorroundingDistance = 0.1f;
    }

    public enum BreslavZComputingMode
    {
        PaperMode,
        JotLibMode
    }
}
