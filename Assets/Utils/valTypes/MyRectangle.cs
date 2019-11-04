using System;
using System.Collections.Generic;
using Assets.Ring2;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.valTypes
{
    [Serializable]
    public class MyRectangle : Positions2D<float>
    {
        public MyRectangle(float x, float y, float width, float height) : base(x, y, width, height)
        {
        }

        public MyRectangle(Positions2D<float> other) : base(other.X, other.Y, other.Width, other.Height)
        {
        }


        public Vector2 Center
        {
            get { return new Vector2(X + Width / 2, Y + Height / 2); }
        }

        public float Area => Width * Height;
        public Vector2 Size => new Vector2(Width, Height);

        public MyRectangle DownLeftSubElement()
        {
            return new MyRectangle(X, Y, Width / 2, Height / 2);
        }

        public MyRectangle DownRightSubElement()
        {
            return new MyRectangle(X + Width / 2, Y, Width / 2, Height / 2);
        }

        public MyRectangle TopRightSubElement()
        {
            return new MyRectangle(X, Y + Height / 2, Width / 2, Height / 2);
        }

        public MyRectangle TopLeftSubElement()
        {
            return new MyRectangle(X + Width / 2, Y + Height / 2, Width / 2, Height / 2);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(X, Y, Width, Height);
        }

        public MyRectangle ToRectangle()
        {
            return new MyRectangle(X, Y, Width, Height);
        }

        public bool Contains(Vector2 position)
        {
            return position.x >= X && position.x <= (X + Width) && position.y >= Y && position.y <= (Y + Height);
        }

        public MyRectangle EnlagreByMargins(float margin)
        {
            return new MyRectangle(X - margin, Y - margin, Width + margin * 2,
                Height + margin * 2);
        }

        public IntRectangle ToIntRectange()
        {
            return new IntRectangle(
                Mathf.RoundToInt(X),
                Mathf.RoundToInt(Y),
                Mathf.RoundToInt(Width),
                Mathf.RoundToInt(Height));
        }

        public Vector2 SampleByUv(Vector2 uv)
        {
            return new Vector2( X + uv.x*Width, Y + uv.y * Height);
        }

        public Vector2 TopLeftPoint => new Vector2(X, Y + Height);
        public Vector2 TopRightPoint => new Vector2(X + Width, Y + Height);
        public Vector2 DownLeftPoint => new Vector2(X, Y);
        public Vector2 DownRightPoint => new Vector2(X+Width, Y);
        public List<Vector2> Vertices => new List<Vector2>(){TopLeftPoint, TopRightPoint, DownLeftPoint, DownRightPoint};

        public MyRectangle Scale(float scaling)
        {
            return new MyRectangle(X * scaling, Y * scaling, Width * scaling, Height * scaling);
        }

        public static MyRectangle CenteredAt(Vector2 center, Vector2 size)
        {
            return new MyRectangle(center.x - size.x / 2, center.y - size.y / 2, size.x, size.y);
        }

        public static MyRectangle FromVertex(Vector2 downLeft, Vector2 topRight)
        {
            return new MyRectangle(downLeft.x, downLeft.y, topRight.x - downLeft.x, topRight.y - downLeft.y);
        }

        public float MaxX => X + Width;
        public float MaxY => Y + Height;
        public Vector2 XRange => new Vector2(X, MaxX);
        public Vector2 YRange => new Vector2(Y, MaxY);

        public MyRectangle CenterAt(Vector2 position)
        {
            return new MyRectangle(position.x - Width / 2f, position.y - Height / 2f, Width, Height);
        }

        public MyRectangle SubRectangle(MyRectangle selectorRectangle)
        {
            return new MyRectangle(X + Width*selectorRectangle.X, Y+Height*selectorRectangle.Y, Width*selectorRectangle.Width, Height*selectorRectangle.Height);
        }

        public static bool Intersects(MyRectangle rect1, MyRectangle rect2)
        {
            var a1= (rect1.X < rect2.X + rect2.Width &&
                    rect1.X + rect1.Width > rect2.X &&
                    rect1.Y < rect2.Y + rect2.Height &&
                    rect1.Y + rect1.Height > rect2.Y);
            return a1;
        }

        public static bool IsCompletlyInside(MyRectangle bigger, MyRectangle smaller)
        {
            return bigger.X <= smaller.X &&
                   (bigger.X + bigger.Width) >= (smaller.X + smaller.Width) &&
                   bigger.Y <= smaller.Y &&
                   (bigger.Y + bigger.Height) >= (smaller.Y + smaller.Height);
        }

        public static bool IsInside(MyRectangle rect, Vector2 point)
        {
            return rect.X <= point.x && (rect.X + rect.Width >= point.x) && rect.Y <= point.y && (rect.Y + rect.Height >= point.y);
        }

    }
}