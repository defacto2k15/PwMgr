using System;
using Assets.Utils;
using UnityEngine;

namespace Assets.Roads.Pathfinding.Fitting
{
    public class CurveFunc
    {
        private Func<double, Vector2> _func;

        public CurveFunc(Func<double, Vector2> func)
        {
            _func = func;
        }

        public Vector2 Sample(double t)
        {
            float epsylon = 0.00001f;
            Preconditions.Assert(t >= (0 - epsylon) && t <= (1 + epsylon),
                $"T must be >0 than 0 nad <= than 1 , it is {t}");
            return _func(t);
        }
    }
}