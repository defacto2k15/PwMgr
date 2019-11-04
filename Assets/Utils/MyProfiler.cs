using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils.MT;
using UnityEngine.Profiling;

namespace Assets.Utils
{
    public static class MyProfiler
    {
        public static void BeginSample(string name)
        {
            if (TaskUtils.IsThisUnityTHread)
            {
                Profiler.BeginSample(name);
            }
        }

        public static void EndSample()
        {
            if (TaskUtils.IsThisUnityTHread)
            {
                Profiler.EndSample();
            }
        }
    }

    public class MyNamedProfiler
    {
        private string _name;

        public MyNamedProfiler(string name)
        {
            _name = name;
        }

        public void BeginSample()
        {
            MyProfiler.BeginSample(_name);
        }

        public void EndSample()
        {
            MyProfiler.EndSample();
        }
    }
}