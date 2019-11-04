using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public class RayDrawingDebugObject : MonoBehaviour
    {
        private static List<DebugRayInfo> _list = new List<DebugRayInfo>();

        public static void AddRay(Vector3 startPos, Vector3 dir)
        {
            _list.Add(new DebugRayInfo(startPos, dir));
        }

        private void Update()
        {
            foreach (var ray in _list)
            {
                Debug.DrawRay(ray.StartPos, ray.Dir * 10, Color.red);
            }
        }
    }

    public class DebugRayInfo
    {
        private readonly Vector3 _startPos;
        private readonly Vector3 _dir;

        public DebugRayInfo(Vector3 startPos, Vector3 dir)
        {
            _startPos = startPos;
            _dir = dir;
        }

        public Vector3 StartPos
        {
            get { return _startPos; }
        }

        public Vector3 Dir
        {
            get { return _dir; }
        }
    }
}