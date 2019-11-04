using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.valTypes
{
    public class Positions2D<T>
    {
        private readonly T _x;
        private readonly T _y;
        private readonly T _width;
        private readonly T _height;

        public Positions2D(T x, T y, T width, T height)
        {
            this._x = x;
            this._y = y;
            this._width = width;
            this._height = height;
        }

        public T X
        {
            get { return _x; }
        }

        public T Y
        {
            get { return _y; }
        }

        public T Width
        {
            get { return _width; }
        }

        public T Height
        {
            get { return _height; }
        }

        public T DownLeftX
        {
            get { return X; }
        }

        public T DownLeftY
        {
            get { return Y; }
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<T>.Default.GetHashCode(_x);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(_y);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(_width);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(_height);
                return hashCode;
            }
        }

        protected bool Equals(Positions2D<T> other)
        {
            return EqualityComparer<T>.Default.Equals(_x, other._x) &&
                   EqualityComparer<T>.Default.Equals(_y, other._y) &&
                   EqualityComparer<T>.Default.Equals(_width, other._width) &&
                   EqualityComparer<T>.Default.Equals(_height, other._height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Positions2D<T>) obj);
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Width: {2}, Height: {3}", _x, _y, _width, _height);
        }
    }
}