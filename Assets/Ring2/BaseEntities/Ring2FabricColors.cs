using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.Ring2.BaseEntities
{
    public class Ring2FabricColors
    {
        private List<Color> _colors;

        public Ring2FabricColors(List<Color> colors)
        {
            Preconditions.Assert(colors.Any(), "There is no colors in list");
            Preconditions.Assert(colors.Count < 5, "There is more than 4 colors in list");

            _colors = colors.ToList();
        }

        public List<Color> Colors
        {
            get { return _colors; }
        }
    }
}