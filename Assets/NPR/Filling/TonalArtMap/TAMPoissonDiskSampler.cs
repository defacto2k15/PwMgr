using System.Collections.Generic;
using System.Linq;
using Assets.Grass2;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMPoissonDiskSampler //bazując na http://www.redblobgames.com/articles/noise/introduction.html
    {
        public List<Vector2> Generate(float generationCount, float exclusionRadius, int seed, List<Vector2> previousPoints=null)
        {
            if (previousPoints == null)
            {
                previousPoints = new List<Vector2>();
            }
            var random = new UnsafeRandomProvider(seed); //todo!
            var offsetGenerationArea = new MyRectangle(0, 0, 1, 1);

            float cellSize = exclusionRadius / Mathf.Sqrt(2);

            var width = 1f;
            var height = 1f;

            var grid = new SingleElementGenerationGrid(
                cellSize,
                Mathf.CeilToInt(width / cellSize), Mathf.CeilToInt(height / cellSize),
                exclusionRadius
            );

            var processList = new GenerationRandomQueue<Vector2>();
            var acceptedPoints = new List<Vector2>(1000);


            var firstPoints = Enumerable.Range(0,10).Select(c => new Vector2(random.Next(0, width), random.Next(0, height))).ToList();
            foreach (var point in firstPoints)
            {
                processList.Add(point);
                acceptedPoints.Add(point);
                if (!grid.IsCellFilled(point))
                {
                    grid.Set(point);
                }
            }


            previousPoints.ForEach(c =>
            {
                if (!grid.IsCellFilled(c))
                {
                    grid.Set(c);
                }
            });

            while (!processList.Empty)
            {
                var point = processList.RandomPop();
                for (int i = 0; i < generationCount; i++)
                {
                    Vector2 newPoint = GenerateRandomPointAround(point, exclusionRadius, random);
                    if (offsetGenerationArea.Contains(newPoint) && !grid.Collides(newPoint))
                    {
                        processList.Add(newPoint);
                        acceptedPoints.Add(newPoint);
                        grid.Set(newPoint);
                    }
                }
            }

            return acceptedPoints;
        }

        private Vector2 GenerateRandomPointAround(Vector2 point, float exclusionRadius, UnsafeRandomProvider random)
        {
            var radius = exclusionRadius * random.Next(0, 2);
            var angle = random.Next(0, 2 * Mathf.PI);

            return new Vector2(
                point.x + radius * Mathf.Cos(angle),
                point.y + radius * Mathf.Sin(angle)
            );
        }
    }
}