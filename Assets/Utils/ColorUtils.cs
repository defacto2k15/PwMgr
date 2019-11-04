using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public static class ColorUtils
    {
        public static Color FromHex(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 1);
        }

        public static Vector3 RgbToHsv(Color color)
        {
            float h;
            float s;
            float v;
            Color.RGBToHSV(color, out h, out s, out v);
            return new Vector3(h, s, v);
        }

        public static Color HsvToRgb(Vector3 hsv)
        {
            return new Color(hsv.x, hsv.y, hsv.z);
        }

        public static Vector3 ToVector3(this Color color)
        {
            return new Vector3(color[0], color[1], color[2]);
        }
    }
}