using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Preparment.MarginMerging
{
    public class HeightmapMargin
    {
        private float[] _marginValues;

        public HeightmapMargin(float[] marginValues)
        {
            _marginValues = marginValues;
        }

        public int Length
        {
            get { return _marginValues.Length; }
        }

        public int WorkingLength
        {
            get { return Length - 1; }
        }

        public float[] MarginValues
        {
            get { return _marginValues; }
        }

        public HeightmapMargin SetLength(int newWorkingLength)
        {
            if (WorkingLength > newWorkingLength)
            {
                Preconditions.Assert(WorkingLength % newWorkingLength == 0,
                    "New margin is not multiplication of current margin");
                var newPointSize = Length / newWorkingLength;
                float[] newValues = new float[newWorkingLength + 1];
                for (var i = 0; i < newWorkingLength; i++)
                {
                    newValues[i] = _marginValues.Skip(i * newPointSize).Take(newPointSize).Sum() /
                                   newPointSize; //average
                }
                newValues[newWorkingLength] = _marginValues[WorkingLength];
                return new HeightmapMargin(newValues);
            }
            else if (WorkingLength < newWorkingLength)
            {
                Preconditions.Assert(newWorkingLength % WorkingLength == 0,
                    "New margin is not multiplication of current margin");
                var newPointSize = newWorkingLength / WorkingLength;
                float[] newValues = new float[newWorkingLength + 1];
                for (var i = 0; i < WorkingLength; i++)
                {
                    for (var j = 0; j < newPointSize; j++)
                    {
                        newValues[i * newPointSize + j] = Mathf.Lerp(_marginValues[i], _marginValues[i + 1],
                            (float) j / newPointSize);
                    }
                }
                newValues[newWorkingLength] = _marginValues[WorkingLength];
                return new HeightmapMargin(newValues);
            }
            return new HeightmapMargin(_marginValues);
        }
    }
}