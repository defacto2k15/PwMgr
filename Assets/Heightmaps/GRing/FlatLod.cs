using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Heightmaps.GRing
{
    public struct FlatLod
    {
        private int _scalarValue;
        private int _sourceQuadLod;

        public FlatLod(int scalarValue, int sourceQuadLod = -1)
        {
            _scalarValue = scalarValue;
            _sourceQuadLod = sourceQuadLod;
        }

        public int ScalarValue => _scalarValue;

        public bool Equals(FlatLod other)
        {
            return _scalarValue == other._scalarValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FlatLod && Equals((FlatLod) obj);
        }

        public override int GetHashCode()
        {
            return _scalarValue;
        }

        public static readonly FlatLod NOT_SET = new FlatLod(-1, -1);

        public int SourceQuadLod => _sourceQuadLod;
    }
}