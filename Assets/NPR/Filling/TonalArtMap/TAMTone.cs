using System.Collections.Generic;
using System.Linq;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMTone
    {
        private TAMTone _lowerTone;
        private TAMStrokeOrientation _strokeOrientation;

        public TAMTone(TAMStrokeOrientation strokeOrientation, TAMTone lowerTone = null)
        {
            _strokeOrientation = strokeOrientation;
            _lowerTone = lowerTone;
        }

        public bool IsLowestTone => _lowerTone == null;
        public TAMTone LowerTone => _lowerTone;

        public static List<TAMTone> CreateList(int count, Dictionary<int, TAMStrokeOrientation> orientationByToneIndex)
        {
            var outList = new List<TAMTone>();
            TAMTone lastTone = null;
            for (int i = 0; i < count; i++)
            {
                var orientation = orientationByToneIndex.OrderByDescending(c => c.Key).First(c => c.Key <= i).Value;

                var newTone = new TAMTone(orientation, lastTone);
                outList.Add(newTone);
                lastTone = newTone;
            }

            return outList;
        }

        public static List<TAMTone> GenerateList(int count, int verticalStartIndex, int bothStartIndex)
        {
            var orientationDir = new Dictionary<int, TAMStrokeOrientation>();
            orientationDir[0] = TAMStrokeOrientation.Horizontal;
            orientationDir[verticalStartIndex] = TAMStrokeOrientation.Vertical;
            orientationDir[bothStartIndex] = TAMStrokeOrientation.Both;
            return CreateList(count, orientationDir);
        }

        public TAMStrokeOrientation StrokeOrientation => _strokeOrientation;
    }
}