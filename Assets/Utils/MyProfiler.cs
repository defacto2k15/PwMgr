using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Diagnostics;
using Assets.Utils.MT;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Assets.Utils
{
    public static class MyProfiler
    {
        private static Stack<MyStopWatch> _mswDict = new Stack<MyStopWatch>();
        private static bool _logProfilerSampling = true;

        public static void BeginSample(string name)
        {
            if (TaskUtils.IsThisUnityTHread)
            {
                if (_logProfilerSampling)
                {
                    var msw = new MyStopWatch();
                    msw.StartSegment(name);
                    _mswDict.Push(msw);
                }
                Profiler.BeginSample(name);
            }
        }

        public static void EndSample()
        {
            if (TaskUtils.IsThisUnityTHread)
            {
                if (_logProfilerSampling)
                {
                    var oldMsw = _mswDict.Pop();
                    var offset = "";
                    if (_mswDict.Any())
                    {
                        offset = Enumerable.Range(0, _mswDict.Count).Select(c => " | ").Aggregate((a, b) => a + b);
                    }

                    Debug.Log("RX: " +offset+ oldMsw.CollectResults());
                }

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