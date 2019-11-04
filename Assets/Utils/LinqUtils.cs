using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics;

namespace Assets.Utils
{
    public static class LinqUtils
    {
        public static IEnumerable<Pair<T>> AdjecentPairs<T>(this IEnumerable<T> e)
        {
            var enumerator = e.GetEnumerator();
            T first = default(T);
            bool firstWasTaken = false;
            while (enumerator.MoveNext())
            {
                if (!firstWasTaken)
                {
                    first = enumerator.Current;
                    firstWasTaken = true;
                }
                else
                {
                    var second = enumerator.Current;
                    yield return new Pair<T>()
                    {
                        A = first,
                        B = second
                    };
                    first = second;
                }
            }
            enumerator.Dispose();
        }

        public static IEnumerable<Pair<T>> AdjecentCirclePairs<T>(this IEnumerable<T> e)
        {
            var enumerator = e.GetEnumerator();
            T first = default(T);
            T globalFirst = first;
            bool firstWasTaken = false;
            while (enumerator.MoveNext())
            {
                if (!firstWasTaken)
                {
                    first = enumerator.Current;
                    globalFirst = first;
                    firstWasTaken = true;
                }
                else
                {
                    var second = enumerator.Current;
                    yield return new Pair<T>()
                    {
                        A = first,
                        B = second
                    };
                    first = second;
                }
            }
            yield return new Pair<T>()
            {
                A = first,
                B = globalFirst,
            };
            enumerator.Dispose();
        }
    }
}