using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public static class StringUtils
    {
        public static string ToString<T>(IEnumerable<T> input)
        {
            var sb = new StringBuilder();
            sb.Append("{ ");
            foreach (var elem in input)
            {
                sb.Append(elem + " , ");
            }
            sb.Append(" }");
            return sb.ToString();
        }

        public static string MyToString<T>(this IEnumerable<T> input)
        {
            return ToString(input);
        }
    }
}