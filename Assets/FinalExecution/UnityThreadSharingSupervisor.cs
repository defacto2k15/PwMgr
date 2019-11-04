using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class UnityThreadSharingSupervisor
    {
        private UnityThreadSharingSupervisorConfiguration _configuration;
        private float _currentTimeSum = 0;
        private float _thisFrameUsedTime = 0;

        private Dictionary<string, ServiceExecutionInfo> _servicesTime = new Dictionary<string, ServiceExecutionInfo>();

        public UnityThreadSharingSupervisor(float currentTimeSum, UnityThreadSharingSupervisorConfiguration configuration)
        {
            _currentTimeSum = currentTimeSum;
            _configuration = configuration;
        }

        public void Update()
        {
            _thisFrameUsedTime = 0;
            var lastFrameTime = Time.deltaTime;
            var delta = lastFrameTime - _configuration.ExpectedTimePerFrame;

            _currentTimeSum += delta;
            _currentTimeSum *= _configuration.TimeSumMultiplier;
            _currentTimeSum += _configuration.TimeSumOffset;
            _currentTimeSum = Mathf.Max(0, _currentTimeSum);
            _currentTimeSum = Mathf.Min(_currentTimeSum, _configuration.MaxTimeSumValue);
        }

        public float CurrentTimeSum => _currentTimeSum;


        //public bool CanExecute(string serviceName)
        //{
        //    if(_currentTimeSum > _configuration.ExecutionForbiddenThreshold)
        //}

        public void RegisterSerivice(string serviceName)
        {
            _servicesTime.Add(serviceName, new ServiceExecutionInfo()
            {
                DoNeedExecution = false,
                TimeUsageFactor = 0
            });
        }

        public void ServiceEndedExecution(string serviceName, float time)
        {
            _thisFrameUsedTime += time;
            _servicesTime[serviceName].TimeUsageFactor += time;
        }

        public void ServiceQueueState(string serviceName, bool needExecution)
        {
            _servicesTime[serviceName].DoNeedExecution = needExecution;
        }


        private class ServiceExecutionInfo
        {
            public float TimeUsageFactor;
            public bool DoNeedExecution;
        }
    }

    public class UnityThreadSharingSupervisorConfiguration
    {
        public float FrameTimePercentageToUse = 0.1f;
        public float ExpectedTimePerFrame = 1 / 80f;
        public float MaxTimeSumValue = 2;
        public float TimeSumMultiplier = 0.99f;
        public float TimeSumOffset = -0.0005f;
        public double ExecutionForbiddenThreshold = 0.4;
    }

}
