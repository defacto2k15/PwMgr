using Assets.Utils.UTUpdating;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Assets.Scheduling
{
    public class MyUtScheduler
    {
        private readonly MyUtSchedulerConfiguration _configuration;
        private List<BaseUTService> _sleepingServices = new List<BaseUTService>();
        private Queue<BaseUTService> _needyServices = new Queue<BaseUTService>();

        private SchedulerStopwatches _stopwatches = new SchedulerStopwatches();
        private MySchedulingLogger _logger = new MySchedulingLogger();

        public MyUtScheduler( MyUtSchedulerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void AddService(BaseUTService service)
        {
            _sleepingServices.Add(service);
        }

        public void StartFrame()
        {
            _stopwatches.FrameStopwatch.Reset();
            _stopwatches.FrameStopwatch.Start();
            //_logger.StartFrame();
        }

        public void Update()
        {
            if (!_configuration.SchedulingEnabled)
            {
                bool thereWasJobDone = false;
                do
                {
                    thereWasJobDone = false;
                    foreach (var service in _sleepingServices)
                    {
                        if (service.HasWorkToDo())
                        {
                            thereWasJobDone = true;
                        }
                        service.Update();
                    }
                    foreach (var service in _needyServices)
                    {
                        if (service.HasWorkToDo())
                        {
                            thereWasJobDone = true;
                        }
                        service.Update();
                    }
                } while (thereWasJobDone);
            }
            else
            {
                List<BaseUTService> newNeedyServices = null;
                foreach (var service in _sleepingServices)
                {
                    if (service.HasWorkToDo())
                    {
                        if (newNeedyServices == null)
                        {
                            newNeedyServices = new List<BaseUTService>();
                        }

                        newNeedyServices.Add(service);
                    }
                }
                if (newNeedyServices != null)
                {
                    foreach (var service in newNeedyServices)
                    {
                        _sleepingServices.Remove(service);
                        _needyServices.Enqueue(service);
                    }
                }

                //TODO take into account time taken in services before
                var beforeFrameTime = (float) _stopwatches.FrameStopwatch.Elapsed.Milliseconds;
                var freeTimeAmount = _configuration.InitialFreeTimePerFrame;
                while (_needyServices.Any() &&  freeTimeAmount > 0)
                {
                    _stopwatches.ServiceStopwatch.Start();
                    var service = _needyServices.Dequeue();
                    service.Update();
                    _stopwatches.ServiceStopwatch.Stop();

                    if (service.HasWorkToDo())
                    {
                        _needyServices.Enqueue(service);
                    }
                    else
                    {
                        _sleepingServices.Add(service);
                    }

                    var timeUsed = (float) _stopwatches.ServiceStopwatch.Elapsed.TotalSeconds;
                    freeTimeAmount -= timeUsed * _configuration.FreeTimeSubtractingMultiplier;

                    _stopwatches.ServiceStopwatch.Reset();
                }

                var frameTime = (float) _stopwatches.FrameStopwatch.Elapsed.Milliseconds;
                //Debug.Log($"Frame time {frameTime} Prev Updating: {beforeFrameTime} Serv time {frameTime-beforeFrameTime}");

            }
        }

        public List<UTServiceProfileInfo> GetUtServicesProfileInfo()
        {
            return _sleepingServices.Select(c => c.GetServiceProfileInfo())
                .Union(_needyServices.Select(c => c.GetServiceProfileInfo())).ToList();
        }

        private class SchedulerStopwatches
        {
            public Stopwatch FrameStopwatch = new Stopwatch();
            public Stopwatch ServiceStopwatch = new Stopwatch();
        }
    }

    public class MyUtSchedulerConfiguration
    {
        public double FreeTimeAmountAllowingForUpdate = 1 / 50f;
        public float TargetFrameTime = 1 / 30f;
        public float InitialFreeTimePerFrame = 1 / 50f;
        public float FreeTimeSubtractingMultiplier = 1;
        public bool SchedulingEnabled = true;
    }

    public class MySchedulingLogger
    {
        private List<SchedulingFrameInfo> _frameInfos = new List<SchedulingFrameInfo>();
        private SchedulingFrameInfo _currentInfo = new SchedulingFrameInfo();
        private bool _isActive = true;

        public void StartFrame()
        {
            if (_isActive)
            {
                _currentInfo.WholeFrameTime = Time.deltaTime;
                _frameInfos.Add(_currentInfo);
                _currentInfo = new SchedulingFrameInfo();
            }
        }

        public void StopFrame(float servicingTime)
        {
            if (_isActive)
            {
                _currentInfo.ServicesFrameTime = servicingTime;
            }
        }

        public void RegisterServiceWork(BaseUTService service, float time)
        {
            if (_isActive)
            {
                if (_currentInfo.UsedTimeByServices == null)
                {
                    _currentInfo.UsedTimeByServices = new Dictionary<BaseUTService, float>();
                }
                _currentInfo.UsedTimeByServices[service] = time;
            }
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void WriteToFile(string path)
        {
            var sb = new StringBuilder();
            sb.Append("FrameNo;");
            sb.Append("FrameTime;");
            sb.Append("ServicesFT;");
            var serviceToColumn = new Dictionary<BaseUTService, int>();

            int i = 0;
            foreach (var service in _frameInfos.Where(c => c.UsedTimeByServices != null)
                .SelectMany(c => c.UsedTimeByServices.Keys).Distinct())
            {
                serviceToColumn[service] = i++;
                sb.Append(service.GetType().ToString() + ";");
            }
            sb.AppendLine();
            var columnsCount = i;

            i = 0;
            foreach (var frameInfo in _frameInfos)
            {
                sb.Append($"{i++};");
                sb.Append(frameInfo.WholeFrameTime + ";");
                sb.Append(frameInfo.ServicesFrameTime+ ";");

                float[] servicesTime = new float[columnsCount];
                if (frameInfo.UsedTimeByServices != null)
                {
                    foreach (var pair in frameInfo.UsedTimeByServices)
                    {
                        servicesTime[serviceToColumn[pair.Key]] = pair.Value;
                    }
                }

                foreach (var time in servicesTime)
                {
                    sb.Append(time + ";");
                }
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }

        public class SchedulingFrameInfo
        {
            public Dictionary<BaseUTService, float> UsedTimeByServices;
            public float WholeFrameTime;
            public float ServicesFrameTime;
        }
    }
}
