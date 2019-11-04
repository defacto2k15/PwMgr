using System.Collections.Generic;
using System.Linq;

namespace Assets.Heightmaps.Preparment.MarginMerging
{
    class ApexPointData
    {
        private readonly Point2D _apexPoint;
        private readonly MarginDataOfSubmap _downLeft;
        private readonly MarginDataOfSubmap _downRight;
        private readonly MarginDataOfSubmap _topLeft;
        private readonly MarginDataOfSubmap _topRight;

        public ApexPointData(Point2D apexPoint, MarginDataOfSubmap downLeft, MarginDataOfSubmap downRight,
            MarginDataOfSubmap topLeft, MarginDataOfSubmap topRight)
        {
            _apexPoint = apexPoint;
            _downLeft = downLeft;
            _downRight = downRight;
            _topLeft = topLeft;
            _topRight = topRight;
        }

        public void IntegrateApexPoint()
        {
            var allSubmaps = (new List<MarginDataOfSubmap> {_downLeft, _downRight, _topLeft, _topRight})
                .Where(x => x != null).ToList();
            if (allSubmaps.Count != 0)
            {
                var highestPointValue =
                    allSubmaps.Where(x => x.SubmapPosition.IsApexPoint(_apexPoint))
                        .Select(x => x.GetApexHeight(_apexPoint))
                        .Union(
                            allSubmaps.Where(x => !x.SubmapPosition.IsApexPoint(_apexPoint))
                                .Select(x => x.GetHeight(_apexPoint)))
                        .OrderByDescending(x => x).First();

                var biggestLod = allSubmaps.Select(x => x.LodFactor).OrderByDescending(x => x).First();
                foreach (var subMap in allSubmaps)
                {
                    if (subMap.SubmapPosition.IsApexPoint(_apexPoint))
                    {
                        subMap.SetApexHeight(_apexPoint, highestPointValue, biggestLod);
                    }
                }
            }
        }
    }
}