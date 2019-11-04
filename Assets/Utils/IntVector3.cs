using System;
using System.Collections.Generic;

namespace Assets.Utils
{
    [Serializable]
    public struct IntVector3
    {
        public int X;
        public int Y;
        public int Z;

        public IntVector3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        private sealed class XYZEqualityComparer : IEqualityComparer<IntVector3>
        {
            public bool Equals(IntVector3 x, IntVector3 y)
            {
                return x.X == y.X && x.Y == y.Y && x.Z == y.Z;
            }

            public int GetHashCode(IntVector3 obj)
            {
                unchecked
                {
                    var hashCode = obj.X;
                    hashCode = (hashCode * 397) ^ obj.Y;
                    hashCode = (hashCode * 397) ^ obj.Z;
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<IntVector3> XYZComparer { get; } = new XYZEqualityComparer();
    }
}