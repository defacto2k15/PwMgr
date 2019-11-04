using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    class MapAreaPosition
    {
        private readonly Vector3 _downLeft;
        private readonly Vector2 _size;

        public MapAreaPosition(Vector2 size, Vector3 downLeft)
        {
            this._size = size;
            this._downLeft = downLeft;
        }

        public Vector3 DownLeft
        {
            get { return _downLeft; }
        }

        public Vector2 Size
        {
            get { return _size; }
        }

        public Vector3 Center
        {
            get { return DownLeft + (new Vector3(Size.x, 0, Size.y) / 2); }
        }

        protected bool Equals(MapAreaPosition other)
        {
            return _size.Equals(other._size) && _downLeft.Equals(other._downLeft);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapAreaPosition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_size.GetHashCode() * 397) ^ _downLeft.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("DownLeft: {0}, Size: {1}", DownLeft, Size);
        }
    }
}