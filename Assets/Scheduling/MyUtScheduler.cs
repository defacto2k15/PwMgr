using System;
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
        private Queue<float> _previousFramesUpdateTime;
        private MySchedulingLogger _logger = new MySchedulingLogger();

        public MyUtScheduler( MyUtSchedulerConfiguration configuration)
        {
            _configuration = configuration;
            _previousFramesUpdateTime = new Queue<float>(Enumerable.Range(0, _configuration.FramesToTrackTimeCount).Select(c=>_configuration.AvgTimePerFrameInMS).ToList());
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

                var thisFrameUsedServiceUpdates = new List<ServiceWithTime>();
                
                var beforeFrameTime = (float) _stopwatches.FrameStopwatch.Elapsed.Milliseconds;

                var trackedUsedUpTime = _previousFramesUpdateTime.Sum();
                var trackedAverageUpdateTime = trackedUsedUpTime / _configuration.FramesToTrackTimeCount;

                var freeTimeAmount = 2 * _configuration.AvgTimePerFrameInMS - trackedAverageUpdateTime;

                int updatesCount = 0;

                while (_needyServices.Any() && freeTimeAmount > 0)
                {
                    var currentNeedyService = _needyServices.Dequeue();
                    while (currentNeedyService.HasWorkToDo() && freeTimeAmount > 0)
                    {
                        var timeUsedInMs = currentNeedyService.Update();

                        freeTimeAmount -= timeUsedInMs * _configuration.FreeTimeSubtractingMultiplier;
                        _stopwatches.ServiceStopwatch.Reset();

                        thisFrameUsedServiceUpdates.Add(new ServiceWithTime() {Service = currentNeedyService, TotalMiliseconds = timeUsedInMs});
                        updatesCount++;
                    }

                    if (currentNeedyService.HasWorkToDo())
                    {
                        _needyServices.Enqueue(currentNeedyService);
                    }
                    else
                    {
                        _sleepingServices.Add(currentNeedyService);
                    }
                }

                var frameTime = (float) _stopwatches.FrameStopwatch.Elapsed.Milliseconds;
                _previousFramesUpdateTime.Dequeue();
                _previousFramesUpdateTime.Enqueue(frameTime);

                var freeTimeAmount2 = 2*_configuration.AvgTimePerFrameInMS - trackedAverageUpdateTime;
                var thisFrameUpdatesTime = thisFrameUsedServiceUpdates.Select(c=>c.TotalMiliseconds).Sum();
                Debug.Log($"Frame {Time.frameCount} FTA2 {freeTimeAmount2}ms Waiting: {_needyServices.Count} updates count {updatesCount} FrameTime {frameTime}ms updatTime" +
                          $" {thisFrameUpdatesTime}ms percent {thisFrameUpdatesTime/frameTime*100}%");

                //var tw = new StreamWriter(@"C:\tmp\updateLogs\log1.txt", true); //TODO to benchmarking class
                //thisFrameUsedServiceUpdates.ForEach(c => tw.WriteLine($"{Time.frameCount},{c.Service.ToString()},{c.TotalMiliseconds}"));
                //tw.Dispose();
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

    public class ServiceWithTime
    {
        public BaseUTService Service;
        public float TotalMiliseconds;
    }

    public class MyUtSchedulerConfiguration
    {
        public float FreeTimeSubtractingMultiplier;
        public float AvgTimePerFrameInMS = 1000* 1 / 50f;
        public bool SchedulingEnabled = true;
        public int FramesToTrackTimeCount = 30;
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
