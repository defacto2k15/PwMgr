using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Pathfinding.Fitting;
using Assets.Utils;
using UnityEngine;

namespace Assets.Roads.Engraving
{
    public class PathProximityArrayGenerator
    {
        private PathProximityArrayGeneratorConfiguration _configuration;

        public PathProximityArrayGenerator(PathProximityArrayGeneratorConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PathProximityArray Generate(List<PathQuantisized> paths, MyRectangle coords)
        {
            var mapSize = _configuration.ArraySize;
            var pixelSize = coords.Width / (mapSize.X - 1); //remember about margin!

            var proximityMap = new PathProximityArray(mapSize.X, mapSize.Y);

            foreach (var aPath in paths)
            {
                var cutPath = aPath.CutSubRectangle(coords.EnlagreByMargins(_configuration.MaximumProximity));
                if (!cutPath.PathNodes.Any())
                {
                    continue;
                }
                var pathEnvelope = cutPath.Envelope.EnlagreByMargins(_configuration.MaximumProximity);

                for (int x = 0; x < mapSize.X; x++)
                {
                    for (int y = 0; y < mapSize.Y; y++)
                    {
                        var globalPosition = new Vector2((x + 0.5f) * pixelSize, (y + 0.5f) * pixelSize) +
                                             new Vector2(coords.X, coords.Y);
                        if (pathEnvelope.Contains(globalPosition))
                        {
                            var proximityInfo = cutPath.ProximityAtPoint(globalPosition);
                            proximityMap.SetProximity(x, y, proximityInfo);
                        }
                    }
                }
            }
            return proximityMap;
        }

        public class PathProximityArrayGeneratorConfiguration
        {
            public float MaximumProximity = RoadDefaultConstants.MaxProximity;
            public IntVector2 ArraySize = RoadDefaultConstants.ProximityArraySize;
        }
    }
}