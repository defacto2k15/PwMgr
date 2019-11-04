using System;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    [Serializable]
    public class TerrainDetailResolution
    {
        [SerializeField] private float _pixelsPerMeter;

        private TerrainDetailResolution(float pixelsPerMeter)
        {
            _pixelsPerMeter = pixelsPerMeter;
        }

        public float PixelsPerMeter
        {
            get { return _pixelsPerMeter; }
        }

        public float MetersPerPixel
        {
            get { return 1 / _pixelsPerMeter; }
        }

        public static TerrainDetailResolution FromPixelsPerMeter(float pixelsPerMeter)
        {
            return new TerrainDetailResolution(pixelsPerMeter);
        }

        public static TerrainDetailResolution FromMetersPerPixel(float metersPerPixel)
        {
            return new TerrainDetailResolution(1 / metersPerPixel);
        }

        public static bool operator >(TerrainDetailResolution r1, TerrainDetailResolution r2)
        {
            return r1._pixelsPerMeter > r2.PixelsPerMeter;
        }

        public static bool operator <(TerrainDetailResolution r1, TerrainDetailResolution r2)
        {
            return r1._pixelsPerMeter < r2.PixelsPerMeter;
        }

        public static bool operator >=(TerrainDetailResolution r1, TerrainDetailResolution r2)
        {
            return r1 > r2 || Math.Abs(r1._pixelsPerMeter - r2._pixelsPerMeter) < 0.001f;
        }

        public static bool operator <=(TerrainDetailResolution r1, TerrainDetailResolution r2)
        {
            return r1 < r2 || Math.Abs(r1._pixelsPerMeter - r2._pixelsPerMeter) < 0.001f;
        }


        protected bool Equals(TerrainDetailResolution other)
        {
            return _pixelsPerMeter.Equals(other._pixelsPerMeter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TerrainDetailResolution) obj);
        }

        public override int GetHashCode()
        {
            return _pixelsPerMeter.GetHashCode();
        }
    }
}