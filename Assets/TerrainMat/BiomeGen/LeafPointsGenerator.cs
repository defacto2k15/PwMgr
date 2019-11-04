using System.Collections.Generic;
using Assets.Random;
using UnityEngine;

namespace Assets.TerrainMat.BiomeGen
{
    public class LeafPointsGenerator
    {
        public List<Vector2> GenerateLeafPoints(int randomSeed,
            RandomCharacteristics leafPointsCountCharacteristics,
            RandomCharacteristics leafPointDistanceCharacteristics) //todo jakiś random provider
        {
            var random = new RandomProvider(randomSeed);
            var pointsCount = (int) Mathf.Clamp((float) random.RandomGaussian(leafPointsCountCharacteristics), 3f,
                99999f);
            List<Vector2> outPoints = new List<Vector2>();
            for (int i = 0; i < pointsCount; i++)
            {
                var angle = random.Next(0f, 2 * Mathf.PI);
                var distanceFromStart = random.RandomGaussian(leafPointDistanceCharacteristics);

                var point = new Vector2(
                    (float) (Mathf.Cos(angle) * distanceFromStart),
                    (float) (Mathf.Sin(angle) * distanceFromStart)
                );
                outPoints.Add(point);
            }
            return outPoints;
        }
    }
}