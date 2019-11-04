using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public class MarginedRange
    {
        private Vector2 _baseRange;
        private readonly float _margin;

        public MarginedRange(Vector2 baseRange, float margin)
        {
            _baseRange = baseRange;
            _margin = margin;
        }

        public float PresenceFactor(float value)
        {
            if (VectorUtils.IsBetween(value, _baseRange))
            {
                return 1;
            }
            else
            {
                float difference = 0;
                if (value < _baseRange[0])
                {
                    difference = _baseRange[0] - value;
                }
                else
                {
                    difference = value - _baseRange[1];
                }
                return Mathf.InverseLerp(_margin, 0, difference);
            }
        }

        public float BaseMin => _baseRange.x;
        public float BaseMax => _baseRange.y;
        public float Margin => _margin;
    }
}