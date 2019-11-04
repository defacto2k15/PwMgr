using System.Collections.Generic;
using Assets.Measuring.Gauges;
using UnityEngine;

namespace Assets.Measuring.Scenarios
{
    public class MSampledLineRenderer
    {
        private GameObject _curveBallsRoot;
        private List<Vector3> _debugDrawCurvePoints = new List<Vector3>();
        private GameObject _curvePositionsRoot;
        private Camera _cam;

        public MSampledLineRenderer(Camera cam)
        {
            _cam = cam;
        }

        public void Update()
        {
            if (_debugDrawCurvePoints != null)
            {
                for (int i = 0; i < _debugDrawCurvePoints.Count - 1; i++)
                {
                    var p0 = _debugDrawCurvePoints[i];
                    var p1 = _debugDrawCurvePoints[i+1];
                    Debug.DrawLine(p0,p1, Color.green);
                }
            }
        }

        public void RenderLine(LinesLayoutResult layoutResult, uint lineId)
        {
            RenderLineWithBalls(layoutResult, lineId);
        }

        public void RenderHatchPixels(LinesLayoutResult layoutResult, uint lineId)
        {
            RenderHatchPixelsWithBalls(layoutResult, lineId);
        }

        public void RenderAllHatchesPixels(LinesLayoutResult lastLinesLayoutResult)
        {
            if (_curvePositionsRoot!= null)
            {
                GameObject.Destroy(_curvePositionsRoot);
            }
            _curvePositionsRoot = new GameObject("RootDebugCurvePositions");
            foreach (var pair in lastLinesLayoutResult.Samples)
            {
                GenerateCurvePositionBalls(pair.Value, pair.Key)
                    .transform.SetParent(_curvePositionsRoot.transform);
            }
        }

        private void RenderLineWithBalls(LinesLayoutResult layoutResult, uint lineId)
        {
            if (_curveBallsRoot != null)
            {
                GameObject.Destroy(_curveBallsRoot);
            }
            _curveBallsRoot = new GameObject("RootDebugBalls");
            if (layoutResult.Curves.ContainsKey(lineId))
            {
                GenerateDebugLineBalls(layoutResult.Curves[lineId], lineId).transform.SetParent(_curveBallsRoot.transform);
            }
            else
            {
                Debug.Log("There is no line of id "+lineId);
            }
        }

        private void RenderHatchPixelsWithBalls(LinesLayoutResult layoutResult, uint lineId)
        {
            if (_curvePositionsRoot!= null)
            {
                GameObject.Destroy(_curvePositionsRoot);
            }
            _curvePositionsRoot = new GameObject("RootDebugCurvePositions");
            if (layoutResult.Samples.ContainsKey(lineId))
            {
                GenerateCurvePositionBalls(layoutResult.Samples[lineId], lineId)
                    .transform.SetParent(_curvePositionsRoot.transform);
            }
            else
            {
                Debug.Log("There is no line of id "+lineId);
            }
        }

        private GameObject GenerateCurvePositionBalls(List<LinesLayoutSample> samples, uint lineId)
        {
            UnityEngine.Random.InitState((int) lineId);
            var color = UnityEngine.Random.ColorHSV();

            var root = new GameObject();
            root.name = lineId.ToString();
            foreach (var aSample in samples)
            {
                var ball = GenerateBall(aSample.WorldSpacePosition, color, 0.02f);
                ball.transform.SetParent(root.transform);
            }

            return root;
        }

        private GameObject GenerateDebugLineBalls(CurveIn3DSpace curve, uint lineId)
        {
            UnityEngine.Random.InitState((int) lineId);
            var color = UnityEngine.Random.ColorHSV();

            var curveRoot = new GameObject();
            curveRoot.name = lineId.ToString();

            var samplesCount = 10;
            for (int i = 0; i < samplesCount; i++)
            {
                var t = (float) i / (float) (samplesCount - 1);

                var ball = GenerateBall(curve.Sample(t), color, 0.1f);
                ball.name = $"T is {t}";
                ball.transform.SetParent(curveRoot.transform);
            }

            return curveRoot;
        }

        private GameObject GenerateBall(Vector3 position, Color color, float scale)
        {
            position -= _cam.transform.forward * 0.1f;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.position = position;
            ball.transform.localScale = new Vector3(scale,scale, scale);
            ball.GetComponent<MeshRenderer>().material.color = color;
            return ball;
        }

    }
}