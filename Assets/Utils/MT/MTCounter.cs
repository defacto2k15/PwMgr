using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.Utils.MT
{
    public class MTCounter
    {
        private int _value = 0;

        public int GetNext()
        {
            return Interlocked.Increment(ref _value);
        }
    }
}