using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using Random = UnityEngine.Random;

namespace Assets.Grass
{
    class RandomTuftGenerator
    {
        public static MyRange GetBasePlantBendingStiffness()
        {
            return new MyRange(0.0f, 1.0f); // todo not use normal random, use gauss distr
        }

        public static MyRange GetBasePlantBendingValue()
        {
            return new MyRange(-0.8f, -0.6f);
        }

        public static float GetPlantBendingStiffness(MyRange basePlantBendingStiffness)
        {
            return basePlantBendingStiffness.Lerp(UnityEngine.Random.value);
        }

        public static float GetPlantBendingValue(MyRange basePlantBendingValue)
        {
            return basePlantBendingValue.Lerp(UnityEngine.Random.value);
        }

        public static int GetTuftCount(MyRange elementsRange)
        {
            return (int) UnityEngine.Random.Range(elementsRange.Min, elementsRange.Max);
        }

        public static float GetPositionOffset()
        {
            return UnityEngine.Random.Range(-0.05f, 0.05f);
        }

        public static int GetMaxTuftCount()
        {
            return UnityEngine.Random.Range(5, 10);
        }

        public static MyRange GetTuftElementsRange()
        {
            var min = 3;
            return new MyRange(min, min + UnityEngine.Random.Range(0, 8));
        }

        public static List<float> GetRandomAngles(int elementsCount, float max)
        {
            return
                Enumerable.Range(1, (int) max)
                    .OrderBy(c => UnityEngine.Random.value)
                    .Take(elementsCount)
                    .Select(c => (float) (c * 2 * Math.PI / max))
                    .ToList();
        }
    }
}