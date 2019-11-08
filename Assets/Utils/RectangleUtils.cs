using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Utils
{
    public static class RectangleUtils
    {
        public static MyRectangle CalculateSubPosition(MyRectangle basePosition,
            MyRectangle subelementRectangle)
        {
            var offsetX = subelementRectangle.X * basePosition.Width;
            var offsetY = subelementRectangle.Y * basePosition.Height;

            return new MyRectangle(basePosition.X + offsetX, basePosition.Y + offsetY,
                basePosition.Width * subelementRectangle.Width, basePosition.Height * subelementRectangle.Height);
        }

        public static Vector2 CalculateSubPosition(MyRectangle basePosition,
            Vector2 uv)
        {
            return new Vector2(basePosition.X + basePosition.Width * uv.x, basePosition.Y + basePosition.Height * uv.y);
        }

        public static MyRectangle CalculateSubelementUv(MyRectangle basePosition,
            MyRectangle subelementRectangle)
        {
            var offsettedSubElement = new Vector2(subelementRectangle.X - basePosition.X,
                subelementRectangle.Y - basePosition.Y);

            var startUvX = offsettedSubElement.x / basePosition.Width;
            var startUvY = offsettedSubElement.y / basePosition.Height;

            var sizeUvX = (subelementRectangle.Width / basePosition.Width);
            var sizeUvY = (subelementRectangle.Height / basePosition.Height);

            return new MyRectangle(startUvX, startUvY, sizeUvX, sizeUvY);
        }

        public static Vector2 CalculateSubelementUv(MyRectangle basePosition, Vector2 subPosition)
        {
            return new Vector2(
                (subPosition.x - basePosition.X) / basePosition.Width,
                (subPosition.y - basePosition.Y) / basePosition.Height
            );
        }

        public static bool IsNormalizedUv(MyRectangle uv)
        {
            return
                MyMathUtils.IsBetween(0, 1, uv.X) &&
                MyMathUtils.IsBetween(0, 1, uv.Y) &&
                MyMathUtils.IsBetween(0, 1, uv.TopRightPoint.x) &&
                MyMathUtils.IsBetween(0, 1, uv.TopRightPoint.y) &&
                MyMathUtils.IsBetween(0, 1, uv.Width) &&
                MyMathUtils.IsBetween(0, 1, uv.Height);
        }

        public static IntVector2 CalculateTextureSize(Vector2 area, Vector2 pixelPerUnit)
        {
            return new IntVector2(
                Mathf.CeilToInt(area.x * pixelPerUnit.x),
                Mathf.CeilToInt(area.y * pixelPerUnit.y)
            );
        }

        public static IntVector2 CalculateTextureSize(Vector2 area, float pixelPerUnit)
        {
            return CalculateTextureSize(area, new Vector2(pixelPerUnit, pixelPerUnit));
        }

        public static MyRectangle MoveBy(MyRectangle rect, Vector2 delta)
        {
            return new MyRectangle(rect.X+delta.x, rect.Y+delta.y, rect.Width, rect.Height);
        }
    }
}