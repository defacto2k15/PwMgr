using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.AI.Areas
{
    public class AIArea : MyRectangle
    {
        public AIArea(float x, float y, float width, float height) : base(x, y, width, height)
        {
        }
        public AIArea(Positions2D<float> other) : base(other)
        {
        }
    }
}
