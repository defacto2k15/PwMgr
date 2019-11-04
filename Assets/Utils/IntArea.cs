using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.Utils
{
    public class IntArea : Positions2D<int>
    {
        public IntArea(int x, int y, int width, int height) : base(x, y, width, height)
        {
        }

        public int MaxX => X + Width;

        public int MaxY => Y + Height;
    }
}