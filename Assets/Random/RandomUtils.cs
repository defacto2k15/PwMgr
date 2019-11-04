using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Random
{
    static class RandomUtils
    {
        public static double RandomGaussian(double mean, double stdDev)
        {
            double u1 = 1.0 - UnityEngine.Random.value; //todo random provider
            double u2 = 1.0 - UnityEngine.Random.value;
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2);
            double randNormal =
                mean + stdDev * randStdNormal;
            return randNormal;
        }

        public static string RandomString(int length = 8)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                var charIdx = UnityEngine.Random.Range(0, chars.Length - 1);
                sb.Append(chars[charIdx]);
            }

            return sb.ToString();
        }

        public static float NextBetween(this System.Random random, double min, double max)
        {
            return (float) (min + (random.NextDouble()) * (max - min));
        }

        public static Vector3 NextVector3(this System.Random random)
        {
            return new Vector3((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble());
        }

        public static Vector2 RandomPointInRectalngle(this MyRectangle rectangle, System.Random random)
        {
            return new Vector2(
                NextBetween(random, rectangle.X, rectangle.MaxX),
                NextBetween(random, rectangle.Y, rectangle.MaxY)
            );
        }
    }
}