using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public class MyRange
    {
        private readonly float _min;
        private readonly float _max;

        public MyRange(float min, float max)
        {
            Preconditions.Assert(min <= max, "Min must be <= Max");
            this._min = min;
            this._max = max;
        }

        public float Min
        {
            get { return _min; }
        }

        public float Max
        {
            get { return _max; }
        }

        public float Lerp(float val)
        {
            Preconditions.Assert(val <= 1.02 && val >= -0.02,
                string.Format("Val must be between 0 and 1, but is {0}", val));
            return Mathf.Lerp(_min, _max, Mathf.Clamp01(val));
        }
    }
}