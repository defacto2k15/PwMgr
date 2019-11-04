using System;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.valTypes
{
    [Serializable]
    public class MyQuantRectangle : Positions2D<int>
    {
        private readonly float _quantLength;

        public MyQuantRectangle(int x, int y, int width, int height, float quantLength) : base(x, y, width, height)
        {
            _quantLength = quantLength;
        }

        protected bool Equals(MyQuantRectangle other)
        {
            return base.Equals(other) && _quantLength.Equals(other._quantLength);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MyQuantRectangle) obj);
        }

        public MyRectangle RealSpaceRectangle => new MyRectangle(X*_quantLength,Y*_quantLength,Width*_quantLength, Height*_quantLength);
        public MyQuantRectangle GetQuantBottomLeftRectangle() 
        {
            AssertThereIsSpaceForSubElement();
            return new MyQuantRectangle(X * 2, Y * 2, Width , Height , _quantLength/2);
        }
        public MyQuantRectangle GetQuantBottomRightRectangle() 
        {
            AssertThereIsSpaceForSubElement();
            return new MyQuantRectangle(X * 2+Width, Y * 2, Width , Height , _quantLength/2);
        }

        public MyQuantRectangle GetQuantTopLeftRectangle() 
        {
            AssertThereIsSpaceForSubElement();
            return new MyQuantRectangle(X * 2, Y * 2+Height, Width , Height , _quantLength/2);
        }
        public MyQuantRectangle GetQuantTopRightRectangle() 
        {
            AssertThereIsSpaceForSubElement();
            return new MyQuantRectangle(X * 2+Width, Y * 2 + Height, Width , Height , _quantLength/2);
        }

        private void AssertThereIsSpaceForSubElement()
        {
            Preconditions.Assert(Width%2==0, $"Width of value {Width} cannot be further divided");
            Preconditions.Assert(Height%2==0, $"Height of value {Height} cannot be further divided");
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ _quantLength.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(_quantLength)}: {_quantLength} realSpaceRectangle:{RealSpaceRectangle.ToString()}";
        }

        public float QuantLength => _quantLength;
    }
}