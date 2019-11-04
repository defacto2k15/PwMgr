using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class CameraFlightDemo : MonoBehaviour
    {
        public float FlightSpeed = 0.00001f;
        private float _flightSum = 0;

        private Camera _camera;
        private FlightTrack _track;

        public void Start()
        {
            _camera = GetComponent<Camera>();

            _track = new FlightTrack(new List<FlightTrackNode>()
            {
                new FlightTrackNode()
                {
                    Position = new Vector3(611.51f, 916.74f, -140.5f),
                    EulerRotation = new Vector3(1, -51, 0),
                },
                new FlightTrackNode()
                {
                    Position = new Vector3(549, 915, -93),
                    EulerRotation = new Vector3(-11.7f, -91.5f, 0),
                },
                new FlightTrackNode()
                {
                    Position = new Vector3(517.45f, 915.22f, -81.24f),
                    EulerRotation = new Vector3(-9.27f, -28.4f, 0),
                },
            });

            _track.GetContinousNode(0).SetTransform(_camera.transform);
        }

        public void Update()
        {
            _flightSum += Time.deltaTime * FlightSpeed;
            _flightSum = Mathf.Repeat(_flightSum, 1);
            var node = _track.GetContinousNode(_flightSum);
            node.SetTransform(_camera.transform);
        }
    }


    public class FlightTrackNode
    {
        public Vector3 Position;
        public Vector3 EulerRotation;

        public void SetTransform(Transform transform)
        {
            transform.position = Position;
            transform.eulerAngles = EulerRotation;
        }
    }

    public class FlightTrack
    {
        private List<FlightTrackSegment> _segments;

        private int _currentSegmentIndex = 0;
        private float _currentSegmentStartLength = 0;
        private float _currentSegmentUsedLength = 0;

        private float _trackLength = 0;

        public FlightTrack(List<FlightTrackNode> nodes)
        {
            _segments = nodes.AdjecentPairs().Select((s) => new FlightTrackSegment()
            {
                StartNode = s.A,
                EndNode = s.B
            }).ToList();
            _segments.Add(new FlightTrackSegment()
            {
                StartNode = nodes.Last(),
                EndNode = nodes.First()
            });
            _trackLength = _segments.Sum(c => c.Length);
        }

        public FlightTrackNode GetContinousNode(float fract)
        {
            fract = Mathf.Repeat(fract, 1);
            FlightTrackSegment currentSegment = null;
            var currentLengthSum = 0f;
            foreach (var aSegment in _segments)
            {
                currentLengthSum += aSegment.Length;
                if (currentLengthSum >= fract * _trackLength)
                {
                    currentSegment = aSegment;
                    break;
                }
            }

            var percentInSegment = (fract * _trackLength - (currentLengthSum - currentSegment.Length)) /
                                   currentSegment.Length;

            return currentSegment.GetContinousNode(percentInSegment);
        }

        private class FlightTrackSegment
        {
            public FlightTrackNode StartNode;
            public FlightTrackNode EndNode;

            public float Length => Vector3.Distance(StartNode.Position, EndNode.Position);

            public FlightTrackNode GetContinousNode(float percentInSegment)
            {
                return new FlightTrackNode()
                {
                    Position = Vector3.Lerp(StartNode.Position, EndNode.Position, percentInSegment),
                    EulerRotation = Vector3.Lerp(StartNode.EulerRotation, EndNode.EulerRotation, percentInSegment)
                };
            }
        }
    }
}