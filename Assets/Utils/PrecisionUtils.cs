using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public class PrecisionUtils
    {
        public static T RetrivePerDistancePrecision<T>(Dictionary<T, float> precisions, float distance)
        {
            var biggestRank = precisions.OrderByDescending(c => c.Value).Select(c => c.Key)
                .First();
            return precisions.OrderBy(c => c.Value)
                .Where(c => c.Value > distance).Select(c => c.Key).DefaultIfEmpty(biggestRank)
                .First();
        }
    }
}