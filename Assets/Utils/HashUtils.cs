using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public static class HashUtils
    {
        public static uint AddToHash(uint a, uint b)
        {
            unchecked
            {
                return ((a + 1) * 397) ^ (b + 1);
            }
        }
    }
}