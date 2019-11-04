using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public static class DictionaryUtils
    {
        public static Dictionary<T, float> NormalizeValues<T>(Dictionary<T, float> input)
        {
            var sum = input.Sum(c => c.Value);

            return input.ToDictionary(c => c.Key, c => c.Value / sum);
        }
    }
}