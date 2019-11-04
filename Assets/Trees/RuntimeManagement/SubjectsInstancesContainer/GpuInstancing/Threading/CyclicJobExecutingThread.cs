using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Threading
{
    public class CyclicJobExecutingThread
    {
        private List<CyclicJobWithWait> _jobs = new List<CyclicJobWithWait>();
        private Queue<CyclicJobWithWait> _jobsToBeAdded = new Queue<CyclicJobWithWait>();
        private AutoResetEvent _haltToken = new AutoResetEvent(false);

        private object _newJobsLock = new object();

        private Thread _thread;

        public void AddJob(CyclicJobWithWait newJob)
        {
            lock (_newJobsLock)
            {
                _jobsToBeAdded.Enqueue(newJob);
                _haltToken.Set();
            }
        }

        public void NonMultithreadUpdate()
        {
            if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
            {
                foreach (var job in _jobsToBeAdded)
                {
                    job.PerCycleAction();
                }
            }
        }

        public void Start()
        {
            if (!TaskUtils.GetGlobalMultithreading())
            {
                return;
            }
            _thread = new Thread(() =>
            {
                try
                {
                    var handlesList = new List<AutoResetEvent> {_haltToken};

                    while (true)
                    {
                        int idx = WaitAny(handlesList);
                        if (idx != 0)
                        {
                            CyclicJobWithWait job = _jobs[idx - 1];
                            job.PerCycleAction();
                        }
                        RecreateWaitHandlesArray(idx, handlesList);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("Cyclic job executor caught exception: "+ ex);
                }
            });
            _thread.Name = "CyclicJobExecutor";
            _thread.Start();
        }

        private int WaitAny(List<AutoResetEvent> events)
        {
            while (true)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].WaitOne(0))
                    {
                        return i;
                    }
                }
            }
        }

        private void RecreateWaitHandlesArray(int calledIdx, List<AutoResetEvent> handlesList)
        {
            // first rotate
            if (calledIdx != 0)
            {
                var calledJobHandle = handlesList[calledIdx];
                handlesList.RemoveAt(calledIdx);
                handlesList.Add(calledJobHandle); //to end of queue
                calledJobHandle.Reset();

                var calledJob = _jobs[calledIdx - 1];
                _jobs.RemoveAt(calledIdx - 1);
                _jobs.Add(calledJob);
            }
            lock (_newJobsLock)
            {
                foreach (var newJob in _jobsToBeAdded)
                {
                    _jobs.Add(newJob);
                    handlesList.Add(newJob.CanExecuteEvent);
                }
                _jobsToBeAdded.Clear();
                _haltToken.Reset();
            }
        }
    }
}