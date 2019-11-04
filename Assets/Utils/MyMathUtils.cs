using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    class MyMathUtils
    {
        static public Vector3 RadToDeg(Vector3 input)
        {
            return new Vector3(Mathf.Rad2Deg * input.x, Mathf.Rad2Deg * input.y, Mathf.Rad2Deg * input.z);
        }

        public static Vector3 DegToRad(Vector3 input)
        {
            return new Vector3(Mathf.Deg2Rad * input.x, Mathf.Deg2Rad * input.y, Mathf.Deg2Rad * input.z);
        }

        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles)
        {
            return Quaternion.Euler(eulerAngles) * (point - pivot) + pivot;
        }

        // returns between 0 and 1
        public static float InvLerp(float min, float max, float value)
        {
            Preconditions.Assert(min >= value, string.Format("Min {0} max {1} value {2} E1", min, max, value));
            Preconditions.Assert(max <= value, string.Format("Min {0} max {1} value {2} E2", min, max, value));
            return (value - min) / (max - min);
        }

        public static Vector3 MultiplyMembers(Vector3 inValue, Vector3 multiplier)
        {
            return new Vector3(inValue.x * multiplier.x, inValue.y * multiplier.y, inValue.z * multiplier.z);
        }

        public static bool IsBetween(float min, float max, float value)
        {
            return value >= min && value <= max;
        }

        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static float IntersectionAreaOfTwoCircles(float radius1, Vector2 center1,  float radius2, Vector2 center2)
        {
            float distance = Vector2.Distance(center1, center2);
            if (distance > radius1 + radius2)
            {
                return 0;
            }
            float r = radius1;
            float R = radius2;
            float d = distance;
            if (R < r)
            {
                // swap
                r = radius2;
                R = radius1;
            }

            var part1 = r * r * Math.Acos((d * d + r * r - R * R) / (2 * d * r));
            var part2 = R * R * Math.Acos((d * d + R * R - r * r) / (2 * d * R));
            var part3 = 0.5 * Math.Sqrt((-d + r + R) * (d + r - R) * (d - r + R) * (d + r + R));

            return (float) (part1 + part2 - part3);
        }
    }
}