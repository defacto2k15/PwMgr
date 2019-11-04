using System;
using UnityEngine;

namespace Assets.Utils
{
    public class MyLine
    {
        private float _a; //Ax + By + C = 0
        private float _b;
        private float _c;

        public MyLine(float a, float b, float c)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        public float DistanceToPoint(Vector2 point)
        {
            return (float) (Math.Abs(_a * point.x + _b * point.y + _c) / Math.Sqrt(_a * _a + _b * _b));
        }

        public static MyLine ComputeFrom(Vector2 vector, Vector2 point)
        {
            float a = 0;
            float b = 0;
            float c = 0;
            if (vector.x == 0)
            {
                a = 1; // x = c
                b = 0;
                c = -point.x;
            }
            else
            {
                // y = mx + c
                var m = vector.y / vector.x;
                a = m;
                b = -1;
                c = -1 * a * point.x + (-1 * b * point.y);
            }
            return new MyLine(a, b, c);
        }

        public static MyLine ComputeFromPoints(Vector2 a, Vector2 b)
        {
            return ComputeFrom(a - b, a);
        }
    }
}