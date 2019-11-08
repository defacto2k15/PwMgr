using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Utils
{
    public static class EnumUtils
    {
        public static List<T> GetValues<T>()
        {
            return new List<T>(Enum.GetValues(typeof(T)).Cast<T>());
        }
    }
}
