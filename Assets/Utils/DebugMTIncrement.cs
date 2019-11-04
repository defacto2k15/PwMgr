using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public static class DebugMTIncrement
    {
        private static Dictionary<string, int> _variables = new Dictionary<string, int>();
        private static object lockObj = new object();

        public static void Increment(string name)
        {
            lock (lockObj)
            {
                if (!_variables.ContainsKey(name))
                {
                    _variables[name] = 0;
                }
                _variables[name]++;
            }
        }

        public static string GenerateResults()
        {
            lock (lockObj)
            {
                return _variables.Select(c => c.Key + ": " + c.Value).Aggregate((a, b) => a + "\n" + b);
            }
        }
    }
}