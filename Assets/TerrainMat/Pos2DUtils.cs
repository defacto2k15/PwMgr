using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.TerrainMat
{
    static class Pos2DUtils
    {
        static public bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            int polygonLength = polygon.Length;
            bool inside = false;
            Vector2 endPoint;
            var end = polygon[polygonLength - 1];
            for (int i = 0; i < polygonLength; i++)
            {
                var start = end;
                endPoint = polygon[i];
                end = endPoint;
                if ((end.y > point.y ^ start.y > point.y) &&
                    ((point.x - end.x) < (point.y - end.y) * (start.x - end.x) / (start.y - end.y)))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}