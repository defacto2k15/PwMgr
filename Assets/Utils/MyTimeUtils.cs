using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Utils
{
    public static class MyTimeUtils
    {
        public static int Current()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            int currentEpochTime = (int) (DateTime.UtcNow - epochStart).TotalSeconds;

            return currentEpochTime;
        }
    }
}
