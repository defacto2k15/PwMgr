using Assets.Random;
using MathNet.Numerics.Random;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class StrokesGenerator
    {
        private StrokesGeneratorConfiguration _generationConfiguration;
        private System.Random _random = new System.Random();
        private int _lastId;

        public StrokesGenerator(StrokesGeneratorConfiguration generationConfiguration)
        {
            _generationConfiguration = generationConfiguration;
            _lastId = 1;
        }

        public TAMStroke CreateRandomStroke(Vector2 center, TAMTone tone)
        {
            var size = (float) _random.Next();
            var length = Mathf.Lerp(_generationConfiguration.StrokeLengthRange.x, _generationConfiguration.StrokeLengthRange.y, size);
            length += (float)_random.NextBetween(_generationConfiguration.StrokeHeightJitterRange.x, _generationConfiguration.StrokeLengthJitterRange.y);
            var height = Mathf.Lerp(_generationConfiguration.StrokeHeightRange.x, _generationConfiguration.StrokeHeightRange.y, size);
            height += (float)_random.NextBetween(_generationConfiguration.StrokeHeightJitterRange.x, _generationConfiguration.StrokeHeightJitterRange.y);

            float rotation;
            if (tone.StrokeOrientation == TAMStrokeOrientation.Horizontal)
            {
                rotation = (float) _random.NextBetween(-1, 1) * _generationConfiguration.MaxRotationJitter;
            }
            else if (tone.StrokeOrientation == TAMStrokeOrientation.Vertical)
            { // strokes vertical
                rotation = (float) _random.NextBetween(-1, 1) * _generationConfiguration.MaxRotationJitter + Mathf.PI/2;
            }
            else
            {
                if (_random.NextBoolean())
                {
                    rotation = (float) _random.NextBetween(-1, 1) * _generationConfiguration.MaxRotationJitter;
                }
                else
                {
                    rotation = (float) _random.NextBetween(-1, 1) * _generationConfiguration.MaxRotationJitter + Mathf.PI / 2;

                }
            }
            return new TAMStroke(center, height, length, rotation, _lastId++);
        }

        public void ResetSeed(int seed)
        {
            _random = new System.Random(seed);
        }

        public TAMStroke UpdatePerMipmapLevelStrokeParameters(TAMStroke tamStroke, TAMMipmapLevel level)
        {
            var reverseIndex = level.MipmapLevelsCount - level.LevelIndex - 1;
            var newHeight = tamStroke.Height * Mathf.Pow(_generationConfiguration.PerMipmapLevelHeightMultiplier, reverseIndex);
            return new TAMStroke(tamStroke.Center, newHeight, tamStroke.Length, tamStroke.Rotation, tamStroke.Id);
        }
    }
}