using UnityEngine;

namespace Assets.Random.Fields
{
    public class Ring2RandomFieldFigureRepositoryConfiguration
    {
        private float _figurePixelsPerUnit;
        private Vector2 _figureSegmentSize;

        public Ring2RandomFieldFigureRepositoryConfiguration(float figurePixelsPerUnit, Vector2 figureSegmentSize)
        {
            _figurePixelsPerUnit = figurePixelsPerUnit;
            _figureSegmentSize = figureSegmentSize;
        }

        public float FigurePixelsPerUnit => _figurePixelsPerUnit;

        public Vector2 FigureSegmentSize => _figureSegmentSize;
    }
}