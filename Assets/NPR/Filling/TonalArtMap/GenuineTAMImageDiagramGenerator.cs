using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class GenuineTAMImageDiagramGenerator : ITAMImageDiagramGenerator
    {
        private GenuineTAMImageDiagramGeneratorConfiguration _generationConfiguration;
        private StrokesGenerator _strokesGenerator;
        private System.Random _random = new System.Random();

        public GenuineTAMImageDiagramGenerator(GenuineTAMImageDiagramGeneratorConfiguration generationConfiguration, StrokesGenerator strokesGenerator)
        {
            _generationConfiguration = generationConfiguration;
            _strokesGenerator = strokesGenerator;
        }

        public TAMImageDiagram Generate(TAMImageDiagram baseDiagram, TAMTone tone, TAMMipmapLevel mipmapLevel, int seed)
        {
            _strokesGenerator.ResetSeed(seed);
            _random = new System.Random(seed);

            var diagram = baseDiagram.Copy();
            float currentCoverage = ComputeCoverage(diagram, mipmapLevel);
            float targetCoverage = _generationConfiguration.TargetCoverages[tone];
            while (targetCoverage > currentCoverage)
            {
                List<RankedPossibleStroke> possibleStrokes = new List<RankedPossibleStroke>();
                for (int i = 0; i < _generationConfiguration.TriesCount[mipmapLevel]; i++) {
                    TAMStroke newStroke = _strokesGenerator.CreateRandomStroke(new Vector2(_random.Next(), _random.Next()),tone);
                    RankedPossibleStroke rankedNewStroke = RankStroke(newStroke, diagram);
                    possibleStrokes.Add(rankedNewStroke);
                }

                var bestStroke = possibleStrokes.OrderBy(p => p.Rank).First();
                diagram.AddStroke(bestStroke.Stroke);
                currentCoverage = ComputeCoverage(diagram, mipmapLevel);
            }

            return diagram;
        }

        private RankedPossibleStroke RankStroke(TAMStroke newStroke, TAMImageDiagram diagram)
        {
            // rank the bigger, the better
            var distanceSum = 0f;
            var intersectionArea = 0f;
            foreach (var stroke in diagram.Strokes)
            {
                var intersection = MyMathUtils.IntersectionAreaOfTwoCircles(newStroke.Length, newStroke.Center, stroke.Length, stroke.Center);
                intersectionArea += intersection;
                distanceSum += Vector2.Distance(newStroke.Center, stroke.Center);
            }

            var intersectionPercent = intersectionArea/(Math.PI * Math.Pow(newStroke.Length, 2));

            var rank = Mathf.Pow(distanceSum, (float) (2 - intersectionPercent));
            return new RankedPossibleStroke()
            {
                Rank = rank,
                Stroke = newStroke
            };
        }

        private float ComputeCoverage(TAMImageDiagram diagram, TAMMipmapLevel mipmapLevel)
        {
            var areaSum = 0.0;
            for (int i = 0; i < diagram.Strokes.Count; i++)
            {
                var currentStroke = diagram.Strokes[i];
                var baseArea = Mathf.PI * Mathf.Pow(currentStroke.Length, 2) * 0.2; //todo

                for (int k = 0; k < i; k++)
                {
                    var otherStroke = diagram.Strokes[k];
                    var intersectionArea =
                        MyMathUtils.IntersectionAreaOfTwoCircles(currentStroke.Length, currentStroke.Center, otherStroke.Length, otherStroke.Center);
                    baseArea -= intersectionArea;
                }

                areaSum += Math.Max(0.001f, baseArea); //todo what to do with other mipmapLevels
            }
            return (float) areaSum;
        }


        private class RankedPossibleStroke
        {
            public TAMStroke Stroke;
            public float Rank;
        }

        public TAMImageDiagram UpdatePerMipmapLevelStrokeParameters(TAMImageDiagram diagram, TAMMipmapLevel level)
        {
            return new TAMImageDiagram( diagram.Strokes.Select(c => _strokesGenerator.UpdatePerMipmapLevelStrokeParameters(c,  level)).ToList());
        }
    }
}