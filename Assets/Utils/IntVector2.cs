using System;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Utils
{
    [Serializable]
    public struct IntVector2
    {
        public int X;
        public int Y;

        public IntVector2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static IntVector2 operator +(IntVector2 v1, IntVector2 v2)
        {
            return new IntVector2(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static IntVector2 operator -(IntVector2 v1, IntVector2 v2)
        {
            return new IntVector2(v1.X - v2.X, v1.Y - v2.Y);
        }

        public int Length => Y - X;


        public bool Equals(IntVector2 other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is IntVector2 && Equals((IntVector2) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public Vector2 ToFloatVec()
        {
            return new Vector2(X, Y);
        }

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
        }

        public static float Distance(IntVector2 d1, IntVector2 d2)
        {
            var delta = d1 - d2;
            return Mathf.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        }

        public static IntVector2 Abs(IntVector2 vec)
        {
            return new IntVector2(Mathf.Abs(vec.X), Mathf.Abs(vec.Y));
        }

        public static IntVector2 operator *(IntVector2 vector, int multiplier)
        {
            return new IntVector2(vector.X * multiplier, vector.Y * multiplier);
        }

        public static IntVector2 operator /(IntVector2 vector, int divider)
        {
            return new IntVector2(vector.X  /divider, vector.Y  / divider);
        }

        public static Vector2 operator *(IntVector2 vector, float multiplier)
        {
            return new Vector2(vector.X * multiplier, vector.Y * multiplier);
        }

        public static IntVector2 operator *(IntVector2 vector1, IntVector2 vector2)
        {
            return new IntVector2(vector1.X*vector2.X, vector1.Y*vector2.Y);
        }

        public static IntVector2 RoundFromFloat(float x, float y)
        {
            return new IntVector2(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        public static IntVector2 RoundFromFloat(Vector2 vector)
        {
            return RoundFromFloat(vector.x, vector.y);
        }

        public static IntVector2 CeilFromFloat(Vector2 vector)
        {
            return new IntVector2(Mathf.CeilToInt(vector.x), Mathf.CeilToInt(vector.y));
        }

        public static IntVector2 FloorFromFloat(Vector2 vector)
        {
            return new IntVector2(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
        }
    }
}