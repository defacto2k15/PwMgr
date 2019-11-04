using System.Collections.Generic;
using System.Linq;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMImageDiagram
    {
        private List<TAMStroke> _strokes;

        public TAMImageDiagram(List<TAMStroke> strokes = null)
        {
            if (strokes == null)
            {
                strokes = new List<TAMStroke>();
            }
            _strokes = strokes;
        }

        public List<TAMStroke> Strokes => _strokes;

        public TAMImageDiagram Copy()
        {
            return new TAMImageDiagram(new List<TAMStroke>(Strokes));
        }

        public void AddStroke(TAMStroke stroke)
        {
            _strokes.Add(stroke);
        }

        public static TAMImageDiagram Combine(TAMImageDiagram first, TAMImageDiagram second)
        {
            return new TAMImageDiagram()
            {
                _strokes = new List<TAMStroke>(first.Strokes).Union(second.Strokes).ToList()
            };
        }

        public static TAMImageDiagram CreateEmpty()
        {
            return new TAMImageDiagram();
        }
    }
}