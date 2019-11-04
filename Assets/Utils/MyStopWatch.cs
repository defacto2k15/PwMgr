using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public class MyStopWatch
    {
        private Dictionary<String, BenchmarkSegment> _usedTime = new Dictionary<string, BenchmarkSegment>();
        private string _currentSegmentName = "default";
        private Stopwatch _sw;
        private int _lastIndex = 0;

        public MyStopWatch()
        {
            _sw = new Stopwatch();
        }

        public void StartSegment(string segmentName)
        {
            StopSegment();
            _currentSegmentName = segmentName;
            _sw.Reset();
            _sw.Start();
        }

        public void StopSegment()
        {
            if (_sw.IsRunning)
            {
                _sw.Stop();
                if (!_usedTime.ContainsKey(_currentSegmentName))
                {
                    _usedTime[_currentSegmentName] = new BenchmarkSegment()
                    {
                        Name = _currentSegmentName,
                        Time = 0,
                        Index = _lastIndex++
                    };
                }
                _usedTime[_currentSegmentName].Time += _sw.ElapsedMilliseconds;
            }
        }

        public string CollectResults()
        {
            StopSegment();
            var sb = new StringBuilder();
            sb.Append("Results: ");
            foreach (var benchmarkSegment in _usedTime.Values.OrderBy(c => c.Index))
            {
                sb.Append(benchmarkSegment.Name + " : " + benchmarkSegment.Time + "ms, ");
            }
            return sb.ToString();
        }

        private class BenchmarkSegment
        {
            public String Name;
            public long Time;
            public int Index;
        }

        private static Dictionary<string, MyStopWatch> _globalDict = new Dictionary<string, MyStopWatch>();

        public static void AddGlobalStopWatch(string name)
        {
            _globalDict[name] = new MyStopWatch();
        }

        public static MyStopWatch RetriveGlobalStopWatch(string name)
        {
            if (!_globalDict.ContainsKey(name))
            {
                AddGlobalStopWatch(name);
            }
            return _globalDict[name];
        }
    }
}