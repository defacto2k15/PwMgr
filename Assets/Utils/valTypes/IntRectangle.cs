using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.Ring2
{
    public class IntRectangle : Positions2D<int>
    {
        public IntRectangle(int x, int y, int width, int height) : base(x, y, width, height)
        {
        }

        public bool Contains(IntVector2 centerPoint)
        {
            return X <= centerPoint.X && MaxX > centerPoint.X && Y <= centerPoint.Y && MaxY > centerPoint.Y;
        }

        public int MaxX
        {
            get { return X + Width; }
        }

        public int MaxY
        {
            get { return Y + Height; }
        }

        public IntRectangle EnlargeByMargins(int margin)
        {
            return new IntRectangle(X - margin, Y - margin, Width + 2 * margin, Height + 2 * margin);
        }

        public IntRectangle BoundBy(IntRectangle boundaries)
        {
            return new IntRectangle(
                Mathf.Max(X, boundaries.X),
                Mathf.Max(Y, boundaries.Y),
                Mathf.Min(MaxX, boundaries.MaxX),
                Mathf.Min(MaxY, boundaries.MaxY)
            );
        }

        public Rect ToRect()
        {
            return new Rect( X, Y, Width, Height);
        }

        public MyRectangle ToFloatRectangle()
        {
            return new MyRectangle(X,Y,Width, Height);
        }
    }
}