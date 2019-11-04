using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    static class VectorUtils
    {
        public static Vector2 To2DPosition(Vector3 inPos)
        {
            return new Vector2(inPos.x, inPos.z);
        }

        public static float FlatDistance(this Vector3 pos1, Vector3 pos2)
        {
            return Vector2.Distance(To2DPosition(pos1), To2DPosition(pos2));
        }

        public static Vector2 MemberwiseMultiply(Vector2 first, Vector2 second)
        {
            return new Vector2(
                first.x * second.x,
                first.y * second.y
            );
        }

        public static Vector3 MemberwiseMultiply(Vector3 first, Vector3 second)
        {
            return new Vector3(
                first.x * second.x,
                first.y * second.y,
                first.x * second.z
            );
        }

        public static Vector2 MemberwiseDivide(Vector2 first, Vector2 second)
        {
            return new Vector2(
                first[0] / second[0],
                first[1] / second[1]
            );
        }

        public static Vector3 MemberwiseDivide(Vector3 first, Vector3 second)
        {
            return new Vector3(
                first[0] / second[0],
                first[1] / second[1],
                first[2] / second[2]
            );
        }

        public static Vector4 MemberwiseDivide(Vector4 first, Vector4 second)
        {
            return new Vector4(
                first[0] / second[0],
                first[1] / second[1],
                first[2] / second[2],
                first[3] / second[3]
            );
        }

        public static float SumMembers(Vector4 input)
        {
            return input[0] + input[1] + input[2] + input[3];
        }

        public static Vector2 Average(Vector2 a, Vector2 b)
        {
            return (a + b) / 2;
        }

        public static Vector3 Add(this Vector3 vec, float value)
        {
            return new Vector3(vec.x + value, vec.y + value, vec.z + value);
        }

        public static Vector2 Add(this Vector2 vec, float value)
        {
            return new Vector2(vec.x + value, vec.y + value);
        }

        public static Vector3 Divide(this Vector3 vec, float value)
        {
            return new Vector3(vec.x / value, vec.y / value, vec.z / value);
        }

        public static IntVector2 ToIntVector(this Vector2 vec)
        {
            return new IntVector2(
                Mathf.RoundToInt(vec.x),
                Mathf.RoundToInt(vec.y)
            );
        }

        public static bool IsBetween(float value, Vector2 range)
        {
            return value >= range[0] && value <= range[1];
        }

        public static Vector2 FillVector2(float f)
        {
            return new Vector2(f, f);
        }

        public static Vector3 FillVector3(float f)
        {
            return new Vector3(f, f, f);
        }

        public static Vector2 CalculateSubelementUv(Vector2 baseElement, Vector2 subelement)
        {
            var offsettedSubElement = new Vector2(subelement.x - baseElement.x, subelement.y - baseElement.x);
            var baseWidth = baseElement.y - baseElement.x;
            return offsettedSubElement / baseWidth;
        }

        public static bool IsNormalized(this Vector2 input)
        {
            return input.x >= 0 && input.y <= 1;
        }


        public static Vector2 CalculateSubPosition(Vector2 basePosition,
            Vector2 uv)
        {
            var baseWidth = basePosition.y - basePosition.x;
            return new Vector2(basePosition.x + baseWidth * uv.x, basePosition.x + baseWidth * uv.y);
        }

        public static IntVector2 ToIntVector2(this Vector2 vec)
        {
            return new IntVector2(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
        }

        public static Vector3 ToVector3(float[] array)
        {
            Preconditions.Assert(array.Length == 3, "Input array must have count 3, but is " + array.Length);
            return new Vector3(array[0], array[1], array[2]);
        }

        public static float[] ToArray(this Vector3 vec)
        {
            return new[] {vec[0], vec[1], vec[2]};
        }

        public static float[] ToArray(this Vector2 vec)
        {
            return new[] {vec[0], vec[1]};
        }

        public static Vector2 XY(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        public static IntVector2 CeilToInt(this Vector2 vec)
        {
            return new IntVector2(Mathf.CeilToInt(vec.x), Mathf.CeilToInt(vec.y));
        }

        public static IntVector2 FloorToInt(this Vector2 vec)
        {
            return new IntVector2(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));
        }

        public static float CrossProduct(Vector2 v1, Vector2 v2)
        {
            return (v1.x * v2.y) - (v1.y * v2.x);
        }

        public static float ManhattanDistance(Vector2 v1, Vector2 v2)
        {
            var delta = v1 - v2;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
        }
    }
}