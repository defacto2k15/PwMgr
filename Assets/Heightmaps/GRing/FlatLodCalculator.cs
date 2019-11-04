using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class FlatLodCalculator
    {
        private readonly UnityCoordsCalculator _coordsCalculator;
        private FlatLodConfiguration _configuration;

        public FlatLodCalculator(UnityCoordsCalculator coordsCalculator,
            FlatLodConfiguration configuration = null)
        {
            if (configuration == null)
            {
                configuration = new FlatLodConfiguration();
            }
            _coordsCalculator = coordsCalculator;
            _configuration = configuration;
        }

        public FlatLod CalculateFlatLod(Ring1Node node, Vector3 cameraPosition)
        {
            var quadLod = node.QuadLodLevel;

            var supportedSet = _configuration.PrecisionsSets.FirstOrDefault(c => c.IsQuadLodSupported(quadLod));
            if (supportedSet == null)
            {
                return new FlatLod(quadLod, quadLod);
            }
            else
            {
                var ringCenter = (_coordsCalculator.CalculateGlobalObjectPosition(node.Ring1Position).Center);
                var distance = Vector2.Distance(ringCenter,
                    new Vector2(cameraPosition.x, cameraPosition.z));
                int minLod = supportedSet.FlatLodPrecisionDistances.Values.Max();
                int appropiateFlatLod =
                    supportedSet.FlatLodPrecisionDistances.OrderByDescending(a => a.Key)
                        .Where(c => c.Key < distance)
                        .Select(c => c.Value)
                        .DefaultIfEmpty(minLod)
                        .First();
                return new FlatLod(appropiateFlatLod, quadLod);
            }
        }
    }

    public class FlatLodConfiguration
    {
        public List<FlatLotPrecisionsSet> PrecisionsSets = new List<FlatLotPrecisionsSet>();
    }

    public class FlatLotPrecisionsSet
    {
        public Dictionary<float, int> FlatLodPrecisionDistances = new Dictionary<float, int>();
        public int MinSupprotedQuadLod;
        public int MaxSupprotedQuadLod;

        public bool IsQuadLodSupported(int quadLod)
        {
            return quadLod >= MinSupprotedQuadLod && quadLod <= MaxSupprotedQuadLod;
        }
    }
}