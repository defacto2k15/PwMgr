using System.Collections.Generic;
using System.Linq;
using Assets.Random;
using UnityEngine;

namespace Assets.Grass2.Billboards
{
    public class Grass2BakedBillboardClan
    {
        private List<int> _bladesCountList;
        private Texture _bladeSeedTextureArray;
        private Texture _detailTextureArray;

        public Grass2BakedBillboardClan(List<int> bladesCountList, Texture bladeSeedTextureArray,
            Texture detailTextureArray)
        {
            _bladesCountList = bladesCountList;
            _bladeSeedTextureArray = bladeSeedTextureArray;
            _detailTextureArray = detailTextureArray;
        }

        public int QueryRandomClosest(int bladesCount, int seed)
        {
            var billboardsOrderedByCount = _bladesCountList.Select((c, i) => new
            {
                index = i,
                difference = Mathf.Abs(c - bladesCount)
            }).OrderBy(c => c.difference).ToList();

            var minDifference = billboardsOrderedByCount.First().difference;
            var ofMinDifference = billboardsOrderedByCount.Where(c => c.difference == minDifference).ToList();

            var random = new RandomProvider(seed);
            return ofMinDifference[random.NextWithMax(0, ofMinDifference.Count - 1)].index;
        }

        public int QueryRandom(int seed)
        {
            var random = new RandomProvider(seed);
            var idx = random.NextWithMax(0, _bladesCountList.Count - 1);
            return idx;
        }

        public Texture BladeSeedTextureArray => _bladeSeedTextureArray;

        public Texture DetailTextureArray => _detailTextureArray;
    }
}