using System.Collections.Generic;
using System.Text;
using OsmSharp.Math.Primitives;
using UnityEngine;

namespace Assets.Roads.Pathfinding.Fitting
{
    public class PathFitterDebugObject2 : MonoBehaviour
    {
        private List<Vector2> _samples;
        private List<Vector2> _nodes;

        public void Start()
        {
            _nodes = new List<Vector2>()
            {
                new Vector2(5, 5),
                new Vector2(10, 3),
                new Vector2(13, 7),
                new Vector2(18, 4),
                new Vector2(22, 9),
                new Vector2(26, 19),
                new Vector2(2, 25),
            };

            var fitter = new PathFitter();

            var pathCurve = fitter.FitPath(_nodes, 5, 3);

            int samplesCount = 70;
            _samples = new List<Vector2>();
            for (int i = 0; i < samplesCount; i++)
            {
                var t = (float) i / (samplesCount - 1);

                _samples.Add(pathCurve.Sample(t));
            }
        }

        public void OnDrawGizmosSelected()
        {
            if (_nodes != null)
            {
                Gizmos.color = Color.red;
                foreach (var node in _nodes)
                {
                    Gizmos.DrawSphere(new Vector3(node.x, node.y, 0), 0.1f);
                }

                Gizmos.color = Color.white;

                for (int i = 0; i < _samples.Count - 1; i++)
                {
                    var p1 = _samples[i];
                    var p2 = _samples[i + 1];

                    Gizmos.DrawLine(new Vector3(p1.x, p1.y, 0), new Vector3(p2.x, p2.y, 0));
                }
            }
        }
    }
}