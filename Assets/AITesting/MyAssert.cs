using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.AITesting
{
    public class MyAssert
    {
        public static void LessThan(float lesser, float bigger)
        {
            if (lesser > bigger)
            {
                throw new MyAssertException($"{lesser} should be smaller than {bigger}");
            }
        }
    }

    public class MyAssertException : Exception
    {
        public MyAssertException(string message) : base(message)
        {
        }
    }
}
