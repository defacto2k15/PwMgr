using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Roads.Pathfinding
{
    public class GratedPathSimplifier
    {
        public List<Vector2> Simplify(List<Vector2> input)
        {
            var outList = new List<Vector2>();

            List<Vector2> output = new List<Vector2>()
            {
                input[0],
                input[1]
            };
            var previousDir = input[1] - input[0];
            var previousPos = input[1];

            foreach (var aPosition in input.Skip(2))
            {
                var dir = aPosition - previousPos;
                if (!dir.Equals(previousDir))
                {
                    output.Add(aPosition);
                    previousDir = dir;
                }
                previousPos = aPosition;
            }

            if (!output.Last().Equals(input.Last()))
            {
                output.Add(input.Last());
            }
            outList = output;

            return outList;
        }
    }
}